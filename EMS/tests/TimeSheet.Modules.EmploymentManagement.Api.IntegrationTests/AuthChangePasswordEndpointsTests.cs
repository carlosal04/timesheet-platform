using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;
using TimeSheet.Modules.EmploymentManagement.Domain.Employees;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class AuthChangePasswordEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string AuthCookieName = "ems.auth";
    private readonly AuthApiFactory _factory;

    public AuthChangePasswordEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ChangePassword_ClearsMustChangePasswordAndUnblocksBusinessRoutes()
    {
        await _factory.ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        await SeedTemporaryPasswordUserAsync(userId, employeeId, "temporary.user@example.com", "TempP@ssw0rd123!");

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "temporary.user@example.com", "TempP@ssw0rd123!");
        var session = await GetSessionAsync(client, authCookie);

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            dbContext.UserSessions.Add(new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionVersion = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10),
                LastSeenAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1)
            });

            await dbContext.SaveChangesAsync();
        });

        Assert.True(session.MustChangePassword);

        using var blockedRequest = new HttpRequestMessage(HttpMethod.Get, "/employees");
        blockedRequest.Headers.Add("Cookie", authCookie);

        var blockedResponse = await client.SendAsync(blockedRequest);
        Assert.Equal(HttpStatusCode.Forbidden, blockedResponse.StatusCode);

        var blockedProblem = await blockedResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(blockedProblem);
        Assert.Equal("Password change required", blockedProblem!.Title);

        using var changePasswordRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/change-password")
        {
            Content = JsonContent.Create(new ChangePasswordRequest("TempP@ssw0rd123!", "UpdatedP@ssw0rd123!"))
        };
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, changePasswordRequest);

        var changePasswordResponse = await client.SendAsync(changePasswordRequest);

        Assert.Equal(HttpStatusCode.NoContent, changePasswordResponse.StatusCode);
        Assert.Contains("set-cookie", changePasswordResponse.Headers.Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
        var updatedAuthCookie = ExtractCookie(changePasswordResponse);

        var updatedSession = await GetSessionAsync(client, updatedAuthCookie);
        Assert.False(updatedSession.MustChangePassword);

        using var employeesRequest = new HttpRequestMessage(HttpMethod.Get, "/employees");
        employeesRequest.Headers.Add("Cookie", updatedAuthCookie);

        var employeesResponse = await client.SendAsync(employeesRequest);
        Assert.Equal(HttpStatusCode.OK, employeesResponse.StatusCode);

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var user = await dbContext.Users.SingleAsync(x => x.Id == userId);
            var revokedSessions = await dbContext.UserSessions
                .Where(x => x.UserId == userId && x.Id != updatedSession.SessionId)
                .ToListAsync();

            Assert.False(user.MustChangePassword);
            Assert.Null(user.TemporaryPasswordExpiresAtUtc);
            Assert.Equal(2, user.SessionVersion);
            Assert.All(revokedSessions, session => Assert.Equal("PasswordChange", session.RevokeReason));
            Assert.Contains(
                dbContext.AuditLogs,
                log => log.ActionType == AuditActionTypes.PasswordChanged
                    && log.EntityId == userId
                    && log.Result == AuditResults.Success);
            Assert.Contains(
                dbContext.AuditLogs,
                log => log.ActionType == AuditActionTypes.SessionRevoked
                    && log.Result == AuditResults.Success
                    && log.MetadataJson!.Contains("PasswordChange"));
        });
    }

    [Fact]
    public async Task ChangePassword_WithInvalidCurrentPassword_ReturnsBadRequest()
    {
        await _factory.ResetDatabaseAsync();

        await SeedTemporaryPasswordUserAsync(Guid.NewGuid(), Guid.NewGuid(), "temporary.invalid@example.com", "TempP@ssw0rd123!");

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "temporary.invalid@example.com", "TempP@ssw0rd123!");

        using var changePasswordRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/change-password")
        {
            Content = JsonContent.Create(new ChangePasswordRequest("WrongCurrent!", "UpdatedP@ssw0rd123!"))
        };
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, changePasswordRequest);

        var changePasswordResponse = await client.SendAsync(changePasswordRequest);

        Assert.Equal(HttpStatusCode.BadRequest, changePasswordResponse.StatusCode);

        var problem = await changePasswordResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Invalid current password", problem!.Title);

        var session = await GetSessionAsync(client, authCookie);
        Assert.True(session.MustChangePassword);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
    }

    private async Task SeedTemporaryPasswordUserAsync(Guid userId, Guid employeeId, string email, string password)
    {
        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var managerRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Manager);

            dbContext.Employees.Add(new Employee
            {
                Id = employeeId,
                FirstName = "Terry",
                LastName = "User",
                Email = email,
                Phone = "5550001234",
                DateOfBirth = new DateOnly(1990, 5, 21),
                HireDate = new DateOnly(2025, 1, 6),
                Status = EmployeeStatusCodes.Active,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            var user = new User
            {
                Id = userId,
                Email = email,
                RoleId = managerRole.Id,
                EmployeeId = employeeId,
                IsActive = true,
                MustChangePassword = true,
                TemporaryPasswordExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(24),
                LastTemporaryPasswordIssuedAtUtc = DateTimeOffset.UtcNow
            };

            user.PasswordHash = passwordHashingService.HashPassword(user, password);
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        });
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.True(payload!.MustChangePassword);

        return ExtractCookie(response);
    }

    private static async Task<SessionResponse> GetSessionAsync(HttpClient client, string authCookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/session");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.NotNull(payload);
        return payload!;
    }

    private static string ExtractCookie(HttpResponseMessage response)
    {
        var header = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith(AuthCookieName + "=", StringComparison.OrdinalIgnoreCase));

        return header.Split(';', 2, StringSplitOptions.TrimEntries)[0];
    }
}
