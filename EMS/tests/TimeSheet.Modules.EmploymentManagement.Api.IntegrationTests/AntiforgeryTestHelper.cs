using System.Net.Http.Json;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

internal static class AntiforgeryTestHelper
{
    private const string AuthCookieName = "ems.auth";

    public static async Task AttachAsync(HttpClient client, string authCookie, HttpRequestMessage request)
    {
        var antiforgery = await GetAsync(client, authCookie);
        request.Headers.Remove("Cookie");
        request.Headers.Add("Cookie", $"{authCookie}; {antiforgery.Cookie}");
        request.Headers.Remove(antiforgery.HeaderName);
        request.Headers.Add(antiforgery.HeaderName, antiforgery.RequestToken);
    }

    public static async Task<(string HeaderName, string RequestToken, string Cookie)> GetAsync(HttpClient client, string authCookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/antiforgery");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AntiforgeryResponse>();
        if (payload is null)
        {
            throw new InvalidOperationException("Anti-forgery response was null.");
        }

        var antiforgeryCookie = response.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2, StringSplitOptions.TrimEntries)[0])
            .Single(value => !value.StartsWith(AuthCookieName + "=", StringComparison.OrdinalIgnoreCase));

        return (payload.HeaderName, payload.RequestToken, antiforgeryCookie);
    }
}
