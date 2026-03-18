using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class AuthSessionRenewEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string AuthCookieName = "ems.auth";
    private readonly AuthApiFactory _factory;

    public AuthSessionRenewEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Renew_WithValidSessionAndAntiforgery_ExtendsExpiryAndKeepsSessionId()
    {
        await _factory.ResetDatabaseAsync();

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        var sessionBefore = await GetSessionAsync(client, authCookie);

        using var renewRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/renew")
        {
            Content = JsonContent.Create(new { })
        };
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, renewRequest);

        var renewResponse = await client.SendAsync(renewRequest);

        Assert.Equal(HttpStatusCode.OK, renewResponse.StatusCode);

        var payload = await renewResponse.Content.ReadFromJsonAsync<RenewSessionResponse>();
        Assert.NotNull(payload);
        Assert.Equal(sessionBefore.SessionId, payload!.SessionId);
        Assert.True(payload.ExpiresAtUtc > sessionBefore.ExpiresAtUtc);
        Assert.Equal(480, payload.IdleTimeoutMinutes);

        var renewedAuthCookie = ExtractCookie(renewResponse);
        var sessionAfter = await GetSessionAsync(client, renewedAuthCookie);
        Assert.Equal(payload.ExpiresAtUtc, sessionAfter.ExpiresAtUtc);

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var session = await dbContext.UserSessions.SingleAsync(x => x.Id == payload.SessionId);

            Assert.Equal(payload.ExpiresAtUtc, session.ExpiresAtUtc);
            Assert.Contains(
                dbContext.AuditLogs,
                log => log.ActionType == AuditActionTypes.SessionRenewed
                    && log.EntityId == payload.SessionId
                    && log.Result == AuditResults.Success);
        });
    }

    [Fact]
    public async Task Renew_AfterLogout_ReturnsUnauthorized()
    {
        await _factory.ResetDatabaseAsync();

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        var antiforgery = await AntiforgeryTestHelper.GetAsync(client, authCookie);

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/logout")
        {
            Content = JsonContent.Create(new { })
        };
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, logoutRequest);

        var logoutResponse = await client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        using var renewRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/renew")
        {
            Content = JsonContent.Create(new { })
        };
        renewRequest.Headers.Add("Cookie", $"{authCookie}; {antiforgery.Cookie}");
        renewRequest.Headers.Add(antiforgery.HeaderName, antiforgery.RequestToken);

        var renewResponse = await client.SendAsync(renewRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, renewResponse.StatusCode);
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
