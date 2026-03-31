using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Email;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Email.Configuration;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Email;

public sealed class SmtpEmailSender(
    IOptions<EmailOptions> emailOptions,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _emailOptions = emailOptions.Value;
    private readonly ILogger<SmtpEmailSender> _logger = logger;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_emailOptions.FromDisplayName, _emailOptions.FromAddress));
        mimeMessage.To.Add(new MailboxAddress(message.To.DisplayName, message.To.Address));
        mimeMessage.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody
        };

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        var secureSocketOptions = MapSecurityMode(_emailOptions.Smtp.SecurityMode);

        await client.ConnectAsync(
            _emailOptions.Smtp.Host,
            _emailOptions.Smtp.Port,
            secureSocketOptions,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(_emailOptions.Smtp.Username))
        {
            await client.AuthenticateAsync(
                _emailOptions.Smtp.Username,
                _emailOptions.Smtp.Password,
                cancellationToken);
        }

        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation(
            "Sent EMS email to {RecipientAddress} with subject {Subject}",
            message.To.Address,
            message.Subject);
    }

    private static SecureSocketOptions MapSecurityMode(EmailSecurityMode securityMode)
        => securityMode switch
        {
            EmailSecurityMode.None => SecureSocketOptions.None,
            EmailSecurityMode.StartTls => SecureSocketOptions.StartTls,
            EmailSecurityMode.SslOnConnect => SecureSocketOptions.SslOnConnect,
            _ => throw new ArgumentOutOfRangeException(nameof(securityMode), securityMode, "Unsupported SMTP security mode.")
        };
}
