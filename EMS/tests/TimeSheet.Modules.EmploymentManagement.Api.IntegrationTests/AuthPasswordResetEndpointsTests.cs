using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class AuthPasswordResetEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string AuthCookieName = "ems.auth";
    private readonly AuthApiFactory _factory;

    public AuthPasswordResetEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ForgotPassword_ForActivatedUser_ReturnsAcceptedAndSendsResetEmail()
    {
        await _factory.ResetDatabaseAsync();
        await SeedActivatedUserAsync("reset.user@example.com", "OldP@ssw0rd123!");

        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/auth/forgot-password", new ForgotPasswordRequest("reset.user@example.com"));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var email = Assert.Single(_factory.GetSentEmails());
        Assert.Equal("reset.user@example.com", email.To.Address);
        Assert.Contains("Reset your EMS password", email.Subject, StringComparison.Ordinal);

        var token = ExtractResetToken(email.TextBody);

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var user = await dbContext.Users.SingleAsync(x => x.Email == "reset.user@example.com");

            Assert.NotNull(user.PasswordResetTokenHash);
            Assert.NotNull(user.PasswordResetTokenExpiresAtUtc);
            Assert.DoesNotContain(token, user.PasswordResetTokenHash!, StringComparison.Ordinal);
            Assert.Contains(
                dbContext.AuditLogs,
                log => log.ActionType == AuditActionTypes.PasswordResetRequested
                    && log.EntityId == user.Id
                    && log.Result == AuditResults.Success);
        });
    }

    [Fact]
    public async Task ForgotPassword_ForUnknownEmail_ReturnsAcceptedWithoutEmail()
    {
        await _factory.ResetDatabaseAsync();

        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("/auth/forgot-password", new ForgotPasswordRequest("missing.user@example.com"));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(_factory.GetSentEmails());
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ChangesPasswordAndRevokesSessions()
    {
        await _factory.ResetDatabaseAsync();
        await SeedActivatedUserAsync("reset.valid@example.com", "OldP@ssw0rd123!");

        using var userClient = CreateClient();
        var authCookie = await LoginAsync(userClient, "reset.valid@example.com", "OldP@ssw0rd123!");

        using var forgotClient = CreateClient();
        var forgotResponse = await forgotClient.PostAsJsonAsync("/auth/forgot-password", new ForgotPasswordRequest("reset.valid@example.com"));
        Assert.Equal(HttpStatusCode.Accepted, forgotResponse.StatusCode);

        var email = Assert.Single(_factory.GetSentEmails());
        var token = ExtractResetToken(email.TextBody);

        using var resetClient = CreateClient();
        var resetResponse = await resetClient.PostAsJsonAsync("/auth/reset-password", new ResetPasswordRequest(token, "NewP@ssw0rd123!"));

        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);

        using var oldSessionRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/session");
        oldSessionRequest.Headers.Add("Cookie", authCookie);

        var oldSessionResponse = await userClient.SendAsync(oldSessionRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, oldSessionResponse.StatusCode);

        var oldLoginResponse = await resetClient.PostAsJsonAsync("/auth/login", new LoginRequest("reset.valid@example.com", "OldP@ssw0rd123!"));
        Assert.Equal(HttpStatusCode.Unauthorized, oldLoginResponse.StatusCode);

        var newLoginResponse = await resetClient.PostAsJsonAsync("/auth/login", new LoginRequest("reset.valid@example.com", "NewP@ssw0rd123!"));
        Assert.Equal(HttpStatusCode.OK, newLoginResponse.StatusCode);

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var user = await dbContext.Users.SingleAsync(x => x.Email == "reset.valid@example.com");

            Assert.Null(user.PasswordResetTokenHash);
            Assert.Null(user.PasswordResetTokenExpiresAtUtc);
            Assert.Contains(
                dbContext.AuditLogs,
                log => log.ActionType == AuditActionTypes.PasswordChanged
                    && log.EntityId == user.Id
                    && log.Result == AuditResults.Success);
            Assert.Contains(
                dbContext.AuditLogs,
                log => log.ActionType == AuditActionTypes.SessionRevoked
                    && log.Result == AuditResults.Success
                    && log.MetadataJson!.Contains("PasswordReset"));
        });
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest()
    {
        await _factory.ResetDatabaseAsync();

        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("/auth/reset-password", new ResetPasswordRequest("invalid-token", "NewP@ssw0rd123!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Invalid or expired reset token", problem!.Title);
    }

    private async Task SeedActivatedUserAsync(string email, string password)
    {
        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var managerRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Manager);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                RoleId = managerRole.Id,
                IsActive = true,
                MustChangePassword = false
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

    private static string ExtractResetToken(string textBody)
    {
        var match = Regex.Match(textBody, @"token=([A-Za-z0-9\-_]+)");
        Assert.True(match.Success, "Expected reset token in email body.");
        return match.Groups[1].Value;
    }
}
