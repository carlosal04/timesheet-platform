namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Name { get; set; } = "Bootstrap Admin";
}
