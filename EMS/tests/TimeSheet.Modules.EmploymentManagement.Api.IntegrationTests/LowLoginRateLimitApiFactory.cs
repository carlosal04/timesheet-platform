namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class LowLoginRateLimitApiFactory : AuthApiFactory
{
    protected override IDictionary<string, string?> GetConfigurationOverrides()
    {
        var overrides = new Dictionary<string, string?>(base.GetConfigurationOverrides())
        {
            ["RateLimiting:Authentication:PermitLimit"] = "2",
            ["RateLimiting:Authentication:WindowMinutes"] = "1",
            ["RateLimiting:Authentication:QueueLimit"] = "0"
        };

        return overrides;
    }
}
