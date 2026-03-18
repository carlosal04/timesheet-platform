using System.Net;
using System.Net.Http.Json;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class AuthEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string AuthCookieName = "ems.auth";
    private readonly AuthApiFactory _factory;

    public AuthEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ReturnsCookieAndAllowsProtectedAuthEndpoint()
    {
        await _factory.ResetDatabaseAsync();

        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest("admin@example.com", "P@ssw0rd123!"));

        await AssertStatusCodeAsync(response, HttpStatusCode.OK);
        Assert.Contains("set-cookie", response.Headers.Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
        var authCookie = ExtractCookie(response);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.Equal("admin@example.com", payload!.Email);
        Assert.Equal("Admin", payload.RoleCode);

        using var antiforgeryRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/antiforgery");
        antiforgeryRequest.Headers.Add("Cookie", authCookie);

        var antiforgeryResponse = await client.SendAsync(antiforgeryRequest);
        Assert.Equal(HttpStatusCode.OK, antiforgeryResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidPayload_ReturnsBadRequest()
    {
        await _factory.ResetDatabaseAsync();

        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(string.Empty, string.Empty));

        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_RevokesCurrentSession()
    {
        await _factory.ResetDatabaseAsync();

        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginRequest("admin@example.com", "P@ssw0rd123!"));
        await AssertStatusCodeAsync(loginResponse, HttpStatusCode.OK);
        var authCookie = ExtractCookie(loginResponse);

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/logout")
        {
            Content = JsonContent.Create(new { })
        };
        logoutRequest.Headers.Add("Cookie", authCookie);

        var logoutResponse = await client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        using var protectedRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/antiforgery");
        protectedRequest.Headers.Add("Cookie", authCookie);

        var protectedResponse = await client.SendAsync(protectedRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
    }

    private static string ExtractCookie(HttpResponseMessage response)
    {
        var header = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith(AuthCookieName + "=", StringComparison.OrdinalIgnoreCase));

        return header.Split(';', 2, StringSplitOptions.TrimEntries)[0];
    }

    private static async Task AssertStatusCodeAsync(HttpResponseMessage response, HttpStatusCode expectedStatusCode)
    {
        if (response.StatusCode == expectedStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == expectedStatusCode,
            $"Expected {(int)expectedStatusCode} {expectedStatusCode} but received {(int)response.StatusCode} {response.StatusCode}.{Environment.NewLine}{body}");
    }
}
