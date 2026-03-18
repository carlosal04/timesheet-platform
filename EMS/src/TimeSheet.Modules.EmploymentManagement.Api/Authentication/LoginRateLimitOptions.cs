namespace TimeSheet.Modules.EmploymentManagement.Api.Authentication;

public sealed class LoginRateLimitOptions
{
    public const string SectionName = "RateLimiting:Authentication";
    public const string PolicyName = "AuthenticationLogin";

    public int PermitLimit { get; set; } = 10;

    public int WindowMinutes { get; set; } = 1;

    public int QueueLimit { get; set; } = 0;
}
