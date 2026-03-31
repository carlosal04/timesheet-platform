namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Configuration;

public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    public string BaseUrl { get; set; } = string.Empty;
}
