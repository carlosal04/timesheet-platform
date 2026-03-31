using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Email;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Email;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Email.Configuration;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Tests.Email;

public class UserAccessEmailComposerTests
{
    private static readonly DateTimeOffset FixedExpiry = new(2026, 4, 1, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ComposeTemporaryPasswordInvite_ReturnsProductionReadyInviteMessage()
    {
        var composer = CreateComposer();

        var message = composer.ComposeTemporaryPasswordInvite(
            new TemporaryPasswordEmailModel(
                new EmailRecipient("developer@example.com", "Dana Developer"),
                "https://ems.local/login",
                "TempPass!234",
                FixedExpiry,
                "Developer",
                IssuedByDisplayName: "Local Admin"));

        Assert.Equal("Your EMS account is ready", message.Subject);
        Assert.Contains("Your EMS account has been created", message.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("TempPass!234", message.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Expires in 24 hours", message.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Dana Developer", message.TextBody, StringComparison.Ordinal);
        Assert.Contains("This message was sent from a no-reply mailbox.", message.TextBody, StringComparison.Ordinal);
    }

    [Fact]
    public void ComposeTemporaryPasswordResend_ExplainsImmediateInvalidation()
    {
        var composer = CreateComposer();

        var message = composer.ComposeTemporaryPasswordResend(
            new TemporaryPasswordEmailModel(
                new EmailRecipient("manager@example.com", "Mina Manager"),
                "https://ems.local/login",
                "TempPass!987",
                FixedExpiry,
                "Manager"));

        Assert.Equal("Your EMS temporary password has been reissued", message.Subject);
        Assert.Contains("replaces all previous temporary passwords immediately", message.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("replaces all previous temporary passwords immediately", message.TextBody, StringComparison.Ordinal);
    }

    [Fact]
    public void ComposePasswordReset_IncludesResetLinkAndEnvironmentFooter()
    {
        var composer = CreateComposer();

        var message = composer.ComposePasswordReset(
            new PasswordResetEmailModel(
                new EmailRecipient("hr@example.com", "Harper HR"),
                "https://ems.local/reset?token=abc",
                FixedExpiry,
                "helpdesk@example.com"));

        Assert.Equal("Reset your EMS password", message.Subject);
        Assert.Contains("https://ems.local/reset?token=abc", message.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("helpdesk@example.com", message.TextBody, StringComparison.Ordinal);
        Assert.Contains("Environment: Development", message.HtmlBody, StringComparison.Ordinal);
    }

    private static UserAccessEmailComposer CreateComposer()
    {
        var options = Options.Create(new EmailOptions
        {
            FromAddress = "no-reply@ems.local",
            FromDisplayName = "Employment Management System",
            SupportEmail = "support@ems.local",
            Smtp = new SmtpOptions
            {
                Host = "localhost",
                Port = 1025,
                SecurityMode = EmailSecurityMode.None
            }
        });

        return new UserAccessEmailComposer(options, new FakeHostEnvironment());
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "EMS.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
