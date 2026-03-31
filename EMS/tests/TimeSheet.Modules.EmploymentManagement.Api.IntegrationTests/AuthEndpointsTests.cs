using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

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
        Assert.False(payload.MustChangePassword);

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
    public async Task Login_WithExpiredTemporaryPassword_ReturnsSpecificUnauthorizedProblem()
    {
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var managerRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Manager);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "expired.temp@example.com",
                RoleId = managerRole.Id,
                IsActive = true,
                MustChangePassword = true,
                TemporaryPasswordExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
                LastTemporaryPasswordIssuedAtUtc = DateTimeOffset.UtcNow.AddHours(-24)
            };

            user.PasswordHash = passwordHashingService.HashPassword(user, "TempP@ssw0rd123!");
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        });

        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest("expired.temp@example.com", "TempP@ssw0rd123!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Temporary password expired", problem!.Title);
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
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, logoutRequest);

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
