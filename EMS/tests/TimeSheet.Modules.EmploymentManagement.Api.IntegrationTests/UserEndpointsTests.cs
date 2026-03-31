using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Domain.Employees;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;
using UserListResult = TimeSheet.Modules.EmploymentManagement.Application.Users.List.Result;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class UserEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string AuthCookieName = "ems.auth";
    private readonly AuthApiFactory _factory;

    public UserEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUsers_ReturnsActiveUsersByDefaultSortedByEmail()
    {
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var managerRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Manager);

            var linkedEmployee = new Employee
            {
                Id = Guid.NewGuid(),
                FirstName = "Brenda",
                LastName = "Stone",
                Email = "brenda.employee@example.com",
                Phone = "5551112222",
                DateOfBirth = new DateOnly(1992, 5, 14),
                HireDate = new DateOnly(2024, 2, 1),
                Status = EmployeeStatusCodes.Active,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            dbContext.Employees.Add(linkedEmployee);

            var alphaUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "alpha.user@example.com",
                RoleId = managerRole.Id,
                IsActive = true
            };
            alphaUser.PasswordHash = passwordHashingService.HashPassword(alphaUser, "P@ssw0rd123!");

            var brendaUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "brenda.user@example.com",
                RoleId = managerRole.Id,
                EmployeeId = linkedEmployee.Id,
                IsActive = true
            };
            brendaUser.PasswordHash = passwordHashingService.HashPassword(brendaUser, "P@ssw0rd123!");

            var inactiveUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "zoe.inactive@example.com",
                RoleId = managerRole.Id,
                IsActive = false
            };
            inactiveUser.PasswordHash = passwordHashingService.HashPassword(inactiveUser, "P@ssw0rd123!");

            dbContext.Users.AddRange(alphaUser, brendaUser, inactiveUser);
            await dbContext.SaveChangesAsync();
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/users");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<UserListResult>();
        Assert.NotNull(payload);
        Assert.Equal(3, payload!.TotalCount);
        Assert.Equal(1, payload.Page);
        Assert.Equal(25, payload.PageSize);
        Assert.Equal(
            payload.Items.Select(x => x.Email).OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            payload.Items.Select(x => x.Email).ToArray());
        Assert.DoesNotContain(payload.Items, x => x.Email == "zoe.inactive@example.com");

        var linkedUser = payload.Items.Single(x => x.Email == "brenda.user@example.com");
        Assert.Equal("Brenda Stone", linkedUser.EmployeeName);
        Assert.True(linkedUser.IsActive);
    }

    [Fact]
    public async Task GetUsers_IncludeInactive_ReturnsInactiveUsers()
    {
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var managerRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Manager);

            var inactiveUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "disabled.user@example.com",
                RoleId = managerRole.Id,
                IsActive = false
            };

            inactiveUser.PasswordHash = passwordHashingService.HashPassword(inactiveUser, "P@ssw0rd123!");
            dbContext.Users.Add(inactiveUser);
            await dbContext.SaveChangesAsync();
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/users?includeInactive=true");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<UserListResult>();
        Assert.NotNull(payload);
        Assert.Contains(payload!.Items, x => x.Email == "disabled.user@example.com" && x.IsActive == false);
    }

    [Fact]
    public async Task GetUsers_EmailAndRoleCodeFilters_WorkTogether()
    {
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var managerRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Manager);
            var adminRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Admin);

            var managerUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "filter.match.manager@example.com",
                RoleId = managerRole.Id,
                IsActive = true
            };
            managerUser.PasswordHash = passwordHashingService.HashPassword(managerUser, "P@ssw0rd123!");

            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "filter.match.admin@example.com",
                RoleId = adminRole.Id,
                IsActive = true
            };
            adminUser.PasswordHash = passwordHashingService.HashPassword(adminUser, "P@ssw0rd123!");

            dbContext.Users.AddRange(managerUser, adminUser);
            await dbContext.SaveChangesAsync();
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/users?email=filter.match&roleCode=Manager");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<UserListResult>();
        Assert.NotNull(payload);
        Assert.Single(payload!.Items);
        Assert.Equal("filter.match.manager@example.com", payload.Items[0].Email);
        Assert.Equal(RoleCodes.Manager, payload.Items[0].RoleCode);
    }

    [Fact]
    public async Task GetUsers_Paging_ReturnsRequestedWindowInSortedOrder()
    {
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var managerRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Manager);

            foreach (var email in new[]
                     {
                         "paging.aaron@example.com",
                         "paging.bravo@example.com",
                         "paging.charlie@example.com",
                         "paging.delta@example.com"
                     })
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    RoleId = managerRole.Id,
                    IsActive = true
                };

                user.PasswordHash = passwordHashingService.HashPassword(user, "P@ssw0rd123!");
                dbContext.Users.Add(user);
            }

            await dbContext.SaveChangesAsync();
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/users?page=2&pageSize=2&email=paging.");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<UserListResult>();
        Assert.NotNull(payload);
        Assert.Equal(4, payload!.TotalCount);
        Assert.Equal(2, payload.Page);
        Assert.Equal(2, payload.PageSize);
        Assert.Equal(
            ["paging.charlie@example.com", "paging.delta@example.com"],
            payload.Items.Select(x => x.Email).ToArray());
    }

    [Fact]
    public async Task GetUsers_ForHrUser_ReturnsForbidden()
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
                Email = "hr.users@example.com",
                RoleId = hrRole.Id,
                IsActive = true
            };

            user.PasswordHash = passwordHashingService.HashPassword(user, "P@ssw0rd123!");
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "hr.users@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/users");
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
