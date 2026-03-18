namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication.Configuration;

public sealed class AuthOptions
{
    public const string SectionName = "Authentication";

    public string CookieName { get; set; } = "ems.auth";

    public int SessionLifetimeMinutes { get; set; } = 480;

    public int LockoutThreshold { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;
}
