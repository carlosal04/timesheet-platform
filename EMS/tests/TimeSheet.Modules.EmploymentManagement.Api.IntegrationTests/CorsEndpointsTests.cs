using System.Net;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class CorsEndpointsTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public CorsEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_ForAllowedOrigin_ReturnsCorsHeaders()
    {
        await _factory.ResetDatabaseAsync();

        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "http://localhost:5173");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("http://localhost:5173", GetSingleHeaderValue(response, "Access-Control-Allow-Origin"));
        Assert.Equal("true", GetSingleHeaderValue(response, "Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task PreflightPost_ForAllowedOrigin_ReturnsCorsHeaders()
    {
        await _factory.ResetDatabaseAsync();

        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/employees");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,x-csrf-token");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:5173", GetSingleHeaderValue(response, "Access-Control-Allow-Origin"));
        Assert.Equal("true", GetSingleHeaderValue(response, "Access-Control-Allow-Credentials"));

        var allowedHeaders = GetSingleHeaderValue(response, "Access-Control-Allow-Headers");
        Assert.Contains("content-type", allowedHeaders, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("x-csrf-token", allowedHeaders, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetHealth_ForDisallowedOrigin_DoesNotReturnCorsHeaders()
    {
        await _factory.ResetDatabaseAsync();

        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "https://not-approved.example");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
    }

    private static string GetSingleHeaderValue(HttpResponseMessage response, string name)
    {
        return Assert.Single(response.Headers.GetValues(name));
    }
}
