using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class AuthLoginRateLimitingEndpointsTests : IClassFixture<LowLoginRateLimitApiFactory>
{
    private readonly LowLoginRateLimitApiFactory _factory;

    public AuthLoginRateLimitingEndpointsTests(LowLoginRateLimitApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WhenConfiguredLimitIsExceeded_ReturnsTooManyRequests()
    {
        await _factory.ResetDatabaseAsync();

        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var firstResponse = await client.PostAsJsonAsync("/auth/login", new LoginRequest("admin@example.com", "wrong-password"));
        var secondResponse = await client.PostAsJsonAsync("/auth/login", new LoginRequest("admin@example.com", "wrong-password"));
        var thirdResponse = await client.PostAsJsonAsync("/auth/login", new LoginRequest("admin@example.com", "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, secondResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, thirdResponse.StatusCode);

        var problem = await thirdResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status429TooManyRequests, problem!.Status);
        Assert.Equal("Too many login attempts", problem.Title);
    }
}
