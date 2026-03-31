namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Email.Configuration;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1025;

    public EmailSecurityMode SecurityMode { get; set; } = EmailSecurityMode.None;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
