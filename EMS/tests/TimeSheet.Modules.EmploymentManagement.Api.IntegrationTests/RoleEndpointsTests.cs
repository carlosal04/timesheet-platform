using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using RoleListResult = TimeSheet.Modules.EmploymentManagement.Application.Roles.List.Result;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class RoleEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string AuthCookieName = "ems.auth";
    private readonly AuthApiFactory _factory;

    public RoleEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRoles_ReturnsCanonicalActiveRolesByDefault()
    {
        await _factory.ResetDatabaseAsync();

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/roles");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<RoleListResult>();
        Assert.NotNull(payload);
        Assert.Equal(4, payload!.Items.Count);
        Assert.Equal(RoleCodes.Admin, payload.Items[0].Code);
        Assert.Equal("Administrator", payload.Items[0].Name);
        Assert.Equal(RoleCodes.Developer, payload.Items[1].Code);
        Assert.Equal("Developer", payload.Items[1].Name);
        Assert.Equal(RoleCodes.HR, payload.Items[2].Code);
        Assert.Equal("Human Resources", payload.Items[2].Name);
        Assert.Equal(RoleCodes.Manager, payload.Items[3].Code);
        Assert.Equal("Manager", payload.Items[3].Name);
        Assert.All(payload.Items, item => Assert.True(item.IsActive));
        Assert.All(payload.Items, item => Assert.True(item.IsSystem));
    }

    [Fact]
    public async Task GetRoles_IncludeInactive_ReturnsInactiveCanonicalRoles()
    {
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var developerRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Developer);
            developerRole.IsActive = false;
            await dbContext.SaveChangesAsync();
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/roles?includeInactive=true");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<RoleListResult>();
        Assert.NotNull(payload);
        Assert.Equal(4, payload!.Items.Count);
        Assert.Contains(payload.Items, item => item.Code == RoleCodes.Developer && item.IsActive == false);
    }

    [Fact]
    public async Task GetRoles_ForHrUser_ReturnsForbidden()
    {
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var hrRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.HR);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "hr.roles@example.com",
                RoleId = hrRole.Id,
                IsActive = true
            };

            user.PasswordHash = passwordHashingService.HashPassword(user, "P@ssw0rd123!");
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "hr.roles@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/roles");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return ExtractCookie(response);
    }

    private static string ExtractCookie(HttpResponseMessage response)
    {
        var header = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith(AuthCookieName + "=", StringComparison.OrdinalIgnoreCase));

        return header.Split(';', 2, StringSplitOptions.TrimEntries)[0];
    }
}
