namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Email.Configuration;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string FromAddress { get; set; } = "no-reply@ems.local";

    public string FromDisplayName { get; set; } = "Employment Management System";

    public SmtpOptions Smtp { get; set; } = new();
}
