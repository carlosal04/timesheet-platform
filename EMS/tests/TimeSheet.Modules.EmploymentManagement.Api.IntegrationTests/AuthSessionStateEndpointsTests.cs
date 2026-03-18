using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Domain.Employees;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class AuthSessionStateEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string AuthCookieName = "ems.auth";
    private readonly AuthApiFactory _factory;

    public AuthSessionStateEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSession_ReturnsCurrentAdminSessionSnapshot()
    {
        await _factory.ResetDatabaseAsync();

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/session");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.NotNull(payload);
        Assert.Equal("admin@example.com", payload!.Email);
        Assert.Equal(RoleCodes.Admin, payload.RoleCode);
        Assert.Null(payload.EmployeeId);
        Assert.True(payload.ExpiresAtUtc > DateTimeOffset.UtcNow);
        Assert.Equal(480, payload.IdleTimeoutMinutes);

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var session = await dbContext.UserSessions.SingleAsync(x => x.Id == payload.SessionId);
            Assert.Equal(session.ExpiresAtUtc, payload.ExpiresAtUtc);
        });
    }

    [Fact]
    public async Task GetSession_ReturnsLinkedEmployeeId_ForBasicUser()
    {
        await _factory.ResetDatabaseAsync();

        var employeeId = Guid.NewGuid();
        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var basicRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Basic);

            dbContext.Employees.Add(new Employee
            {
                Id = employeeId,
                FirstName = "Bea",
                LastName = "Stone",
                Email = "bea.stone@company.com",
                Phone = "5551112222",
                DateOfBirth = new DateOnly(1991, 2, 10),
                HireDate = new DateOnly(2024, 4, 1),
                Status = EmployeeStatusCodes.Active,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "basic.session@example.com",
                RoleId = basicRole.Id,
                EmployeeId = employeeId,
                IsActive = true
            };

            user.PasswordHash = passwordHashingService.HashPassword(user, "P@ssw0rd123!");
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "basic.session@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/session");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.NotNull(payload);
        Assert.Equal(RoleCodes.Basic, payload!.RoleCode);
        Assert.Equal(employeeId, payload.EmployeeId);
    }

    [Fact]
    public async Task GetSession_AfterLogout_ReturnsUnauthorized()
    {
        await _factory.ResetDatabaseAsync();

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/logout")
        {
            Content = JsonContent.Create(new { })
        };
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, logoutRequest);

        var logoutResponse = await client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        using var sessionRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/session");
        sessionRequest.Headers.Add("Cookie", authCookie);

        var sessionResponse = await client.SendAsync(sessionRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, sessionResponse.StatusCode);
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
