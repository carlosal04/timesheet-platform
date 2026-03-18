namespace TimeSheet.Modules.EmploymentManagement.Api.Authentication;

public sealed class ClientNetworkLoginRateLimitKeyProvider : ILoginRateLimitKeyProvider
{
    public string GetPartitionKey(HttpContext httpContext)
    {
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
