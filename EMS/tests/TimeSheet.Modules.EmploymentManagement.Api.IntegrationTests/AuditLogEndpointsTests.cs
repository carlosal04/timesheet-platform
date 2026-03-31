using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AuditLogListResult = TimeSheet.Modules.EmploymentManagement.Application.AuditLogs.List.Result;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class AuditLogEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string AuthCookieName = "ems.auth";
    private readonly AuthApiFactory _factory;

    public AuditLogEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAuditLogs_ReturnsFilteredResultsAndWritesAuditReadEntry()
    {
        await _factory.ResetDatabaseAsync();

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/audit-logs?actionType={AuditActionTypes.LoginSucceeded}&page=1&pageSize=10");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<AuditLogListResult>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload!.Items);
        Assert.All(payload.Items, item => Assert.Equal(AuditActionTypes.LoginSucceeded, item.ActionType));

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var auditReadEntry = await dbContext.AuditLogs
                .OrderByDescending(x => x.OccurredAtUtc)
                .FirstOrDefaultAsync(x => x.ActionType == AuditActionTypes.AuditLogRead);

            Assert.NotNull(auditReadEntry);
            Assert.Equal(AuditEntityTypes.AuditLog, auditReadEntry!.EntityType);
            Assert.Equal(AuditResults.Success, auditReadEntry.Result);
        });
    }

    [Fact]
    public async Task GetAuditLogs_ForHrUser_ReturnsForbidden()
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
                Email = "hr.audit@example.com",
                RoleId = hrRole.Id,
                IsActive = true
            };

            user.PasswordHash = passwordHashingService.HashPassword(user, "P@ssw0rd123!");
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "hr.audit@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/audit-logs");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_WithInvalidActionType_ReturnsBadRequest()
    {
        await _factory.ResetDatabaseAsync();

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/audit-logs?actionType=NotCanonical");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
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
