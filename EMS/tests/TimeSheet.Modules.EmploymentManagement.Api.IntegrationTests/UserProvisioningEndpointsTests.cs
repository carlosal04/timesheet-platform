using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Users;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;
using TimeSheet.Modules.EmploymentManagement.Domain.Employees;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class UserProvisioningEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string AuthCookieName = "ems.auth";
    private readonly AuthApiFactory _factory;

    public UserProvisioningEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateUser_ForLinkedDeveloper_CreatesUserAndSendsInvite()
    {
        await _factory.ResetDatabaseAsync();

        Guid employeeId = Guid.NewGuid();
        Guid developerRoleId = Guid.Empty;

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            developerRoleId = await dbContext.Roles
                .Where(x => x.Code == RoleCodes.Developer)
                .Select(x => x.Id)
                .SingleAsync();

            dbContext.Employees.Add(new Employee
            {
                Id = employeeId,
                FirstName = "Dana",
                LastName = "Coder",
                Email = "dana.coder@example.com",
                Phone = "5551112222",
                DateOfBirth = new DateOnly(1994, 3, 20),
                HireDate = new DateOnly(2025, 6, 1),
                Status = EmployeeStatusCodes.Active,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            await dbContext.SaveChangesAsync();
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/users")
        {
            Content = JsonContent.Create(new CreateUserRequest(developerRoleId, employeeId, null))
        };
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, request);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<CreateUserResponse>();
        Assert.NotNull(payload);
        Assert.Equal("dana.coder@example.com", payload!.Email);
        Assert.Equal(developerRoleId, payload.RoleId);
        Assert.Equal(RoleCodes.Developer, payload.RoleCode);
        Assert.Equal(employeeId, payload.EmployeeId);
        Assert.True(payload.MustChangePassword);
        Assert.True(payload.TemporaryPasswordExpiresAtUtc > DateTimeOffset.UtcNow.AddHours(23));

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var user = await dbContext.Users.SingleAsync(x => x.Id == payload.UserId);

            Assert.Equal("dana.coder@example.com", user.Email);
            Assert.Equal(employeeId, user.EmployeeId);
            Assert.True(user.MustChangePassword);
            Assert.NotNull(user.LastTemporaryPasswordIssuedAtUtc);
            Assert.NotNull(user.TemporaryPasswordExpiresAtUtc);
            Assert.Contains(
                dbContext.AuditLogs,
                log => log.ActionType == AuditActionTypes.UserCreated
                    && log.EntityId == payload.UserId
                    && log.Result == AuditResults.Success);
            Assert.Contains(
                dbContext.AuditLogs,
                log => log.ActionType == AuditActionTypes.TemporaryPasswordIssued
                    && log.EntityId == payload.UserId
                    && log.Result == AuditResults.Success);
        });

        var email = Assert.Single(_factory.GetSentEmails());
        Assert.Equal("dana.coder@example.com", email.To.Address);
        Assert.Contains("Your EMS account is ready", email.Subject, StringComparison.Ordinal);
        Assert.Contains("http://localhost:8088/login", email.TextBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateUser_ForDeveloperWithoutEmployee_ReturnsConflict()
    {
        await _factory.ResetDatabaseAsync();

        Guid developerRoleId = Guid.Empty;
        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            developerRoleId = await dbContext.Roles
                .Where(x => x.Code == RoleCodes.Developer)
                .Select(x => x.Id)
                .SingleAsync();
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/users")
        {
            Content = JsonContent.Create(new CreateUserRequest(developerRoleId, null, "developer@example.com"))
        };
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, request);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Empty(_factory.GetSentEmails());
    }

    [Fact]
    public async Task CreateUser_ForHrUser_ReturnsForbidden()
    {
        await _factory.ResetDatabaseAsync();

        Guid hrRoleId = Guid.Empty;
        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            hrRoleId = await dbContext.Roles.Where(x => x.Code == RoleCodes.HR).Select(x => x.Id).SingleAsync();

            var hrUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "hr.provision@example.com",
                RoleId = hrRoleId,
                IsActive = true
            };

            hrUser.PasswordHash = passwordHashingService.HashPassword(hrUser, "P@ssw0rd123!");
            dbContext.Users.Add(hrUser);
            await dbContext.SaveChangesAsync();
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "hr.provision@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/users")
        {
            Content = JsonContent.Create(new CreateUserRequest(hrRoleId, null, "someone@example.com"))
        };
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, request);

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
