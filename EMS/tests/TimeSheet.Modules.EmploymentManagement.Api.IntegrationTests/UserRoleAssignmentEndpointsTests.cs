using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Users;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class UserRoleAssignmentEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string AuthCookieName = "ems.auth";
    private readonly AuthApiFactory _factory;

    public UserRoleAssignmentEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AssignUserRole_ChangesRoleAndRevokesActiveSessions()
    {
        await _factory.ResetDatabaseAsync();

        Guid targetUserId = Guid.NewGuid();
        string targetEmail = "target.rolechange@example.com";

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var managerRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Manager);

            var user = new User
            {
                Id = targetUserId,
                Email = targetEmail,
                RoleId = managerRole.Id,
                IsActive = true
            };

            user.PasswordHash = passwordHashingService.HashPassword(user, "P@ssw0rd123!");
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        });

        using var targetClient = CreateClient();
        await LoginAsync(targetClient, targetEmail, "P@ssw0rd123!");

        Guid adminRoleId = Guid.Empty;
        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            adminRoleId = await dbContext.Roles
                .Where(x => x.Code == RoleCodes.Admin)
                .Select(x => x.Id)
                .SingleAsync();
        });

        using var adminClient = CreateClient();
        var adminCookie = await LoginAsync(adminClient, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/users/{targetUserId}/role")
        {
            Content = JsonContent.Create(new AssignUserRoleRequest(adminRoleId))
        };
        await AntiforgeryTestHelper.AttachAsync(adminClient, adminCookie, request);

        var response = await adminClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<AssignUserRoleResponse>();
        Assert.NotNull(payload);
        Assert.Equal(targetUserId, payload!.UserId);
        Assert.Equal(adminRoleId, payload.RoleId);
        Assert.Equal(RoleCodes.Admin, payload.RoleCode);
        Assert.Equal(1, payload.SessionsRevoked);

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var user = await dbContext.Users
                .Include(x => x.Role)
                .SingleAsync(x => x.Id == targetUserId);
            var sessions = await dbContext.UserSessions
                .Where(x => x.UserId == targetUserId)
                .OrderBy(x => x.CreatedAtUtc)
                .ToListAsync();

            Assert.Equal(RoleCodes.Admin, user.Role!.Code);
            Assert.Single(sessions);
            Assert.NotNull(sessions[0].RevokedAtUtc);
            Assert.Equal("RoleChange", sessions[0].RevokeReason);
            Assert.Contains(
                dbContext.AuditLogs,
                log => log.ActionType == AuditActionTypes.UserRoleAssigned
                    && log.EntityId == targetUserId
                    && log.Result == AuditResults.Success);
            Assert.Contains(
                dbContext.AuditLogs,
                log => log.ActionType == AuditActionTypes.SessionRevoked
                    && log.EntityId == sessions[0].Id
                    && log.Result == AuditResults.Success);
        });
    }

    [Fact]
    public async Task AssignUserRole_ToInactiveRole_ReturnsConflict()
    {
        await _factory.ResetDatabaseAsync();

        Guid targetUserId = Guid.NewGuid();
        Guid inactiveRoleId = Guid.Empty;

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var managerRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Manager);

            var inactiveRole = new Role
            {
                Id = Guid.NewGuid(),
                Code = "Supervisor",
                Name = "Supervisor",
                IsActive = false,
                IsSystem = false
            };

            inactiveRoleId = inactiveRole.Id;
            dbContext.Roles.Add(inactiveRole);

            var user = new User
            {
                Id = targetUserId,
                Email = "target.inactiverole@example.com",
                RoleId = managerRole.Id,
                IsActive = true
            };

            user.PasswordHash = passwordHashingService.HashPassword(user, "P@ssw0rd123!");
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        });

        using var adminClient = CreateClient();
        var adminCookie = await LoginAsync(adminClient, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/users/{targetUserId}/role")
        {
            Content = JsonContent.Create(new AssignUserRoleRequest(inactiveRoleId))
        };
        await AntiforgeryTestHelper.AttachAsync(adminClient, adminCookie, request);

        var response = await adminClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AssignUserRole_WhenChangeWouldLeaveZeroActiveAdmins_ReturnsConflict()
    {
        await _factory.ResetDatabaseAsync();

        Guid adminUserId = Guid.Empty;
        Guid managerRoleId = Guid.Empty;

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            adminUserId = await dbContext.Users
                .Where(x => x.Email == "admin@example.com")
                .Select(x => x.Id)
                .SingleAsync();
            managerRoleId = await dbContext.Roles
                .Where(x => x.Code == RoleCodes.Manager)
                .Select(x => x.Id)
                .SingleAsync();
        });

        using var adminClient = CreateClient();
        var adminCookie = await LoginAsync(adminClient, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/users/{adminUserId}/role")
        {
            Content = JsonContent.Create(new AssignUserRoleRequest(managerRoleId))
        };
        await AntiforgeryTestHelper.AttachAsync(adminClient, adminCookie, request);

        var response = await adminClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
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
