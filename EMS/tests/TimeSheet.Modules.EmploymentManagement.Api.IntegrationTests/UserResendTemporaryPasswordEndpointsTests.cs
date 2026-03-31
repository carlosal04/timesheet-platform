using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Users;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class UserResendTemporaryPasswordEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string AuthCookieName = "ems.auth";
    private readonly AuthApiFactory _factory;

    public UserResendTemporaryPasswordEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ResendTemporaryPassword_WhenUserIsOnboarding_ReissuesPasswordAndRevokesSessions()
    {
        await _factory.ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        await SeedOnboardingUserAsync(userId, "resend.user@example.com", "TempP@ssw0rd123!", mustChangePassword: true);

        using var userClient = CreateClient();
        var onboardingCookie = await LoginAsync(userClient, "resend.user@example.com", "TempP@ssw0rd123!");

        using var adminClient = CreateClient();
        var adminCookie = await LoginAsync(adminClient, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/users/{userId}/resend-temporary-password")
        {
            Content = JsonContent.Create(new { })
        };
        await AntiforgeryTestHelper.AttachAsync(adminClient, adminCookie, request);

        var response = await adminClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ResendTemporaryPasswordResponse>();
        Assert.NotNull(payload);
        Assert.Equal(userId, payload!.UserId);
        Assert.True(payload.TemporaryPasswordExpiresAtUtc > DateTimeOffset.UtcNow.AddHours(23));

        using var sessionRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/session");
        sessionRequest.Headers.Add("Cookie", onboardingCookie);

        var sessionResponse = await userClient.SendAsync(sessionRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, sessionResponse.StatusCode);

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var user = await dbContext.Users.SingleAsync(x => x.Id == userId);

            Assert.True(user.MustChangePassword);
            Assert.Equal(payload.TemporaryPasswordExpiresAtUtc, user.TemporaryPasswordExpiresAtUtc);
            Assert.NotNull(user.LastTemporaryPasswordIssuedAtUtc);
            Assert.Contains(
                dbContext.AuditLogs,
                log => log.ActionType == AuditActionTypes.TemporaryPasswordResent
                    && log.EntityId == userId
                    && log.Result == AuditResults.Success);
            Assert.Contains(
                dbContext.AuditLogs,
                log => log.ActionType == AuditActionTypes.SessionRevoked
                    && log.Result == AuditResults.Success
                    && log.MetadataJson!.Contains("TemporaryPasswordResend"));
        });

        var email = Assert.Single(_factory.GetSentEmails());
        Assert.Equal("resend.user@example.com", email.To.Address);
        Assert.Contains("reissued", email.Subject, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("replaces all previous temporary passwords immediately", email.TextBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResendTemporaryPassword_WhenUserIsNotOnboarding_ReturnsBadRequest()
    {
        await _factory.ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        await SeedOnboardingUserAsync(userId, "not.onboarding@example.com", "TempP@ssw0rd123!", mustChangePassword: false);

        using var adminClient = CreateClient();
        var adminCookie = await LoginAsync(adminClient, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/users/{userId}/resend-temporary-password")
        {
            Content = JsonContent.Create(new { })
        };
        await AntiforgeryTestHelper.AttachAsync(adminClient, adminCookie, request);

        var response = await adminClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("User is not in onboarding state.", problem!.Title);
    }

    [Fact]
    public async Task ResendTemporaryPassword_WhenUserIsMissing_ReturnsNotFound()
    {
        await _factory.ResetDatabaseAsync();

        using var adminClient = CreateClient();
        var adminCookie = await LoginAsync(adminClient, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/users/{Guid.NewGuid()}/resend-temporary-password")
        {
            Content = JsonContent.Create(new { })
        };
        await AntiforgeryTestHelper.AttachAsync(adminClient, adminCookie, request);

        var response = await adminClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task SeedOnboardingUserAsync(Guid userId, string email, string password, bool mustChangePassword)
    {
        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var managerRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Manager);

            var user = new User
            {
                Id = userId,
                Email = email,
                RoleId = managerRole.Id,
                IsActive = true,
                MustChangePassword = mustChangePassword,
                TemporaryPasswordExpiresAtUtc = mustChangePassword ? DateTimeOffset.UtcNow.AddHours(2) : null,
                LastTemporaryPasswordIssuedAtUtc = mustChangePassword ? DateTimeOffset.UtcNow.AddMinutes(-5) : null
            };

            user.PasswordHash = passwordHashingService.HashPassword(user, password);
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        });
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
