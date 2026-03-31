using System.Globalization;
using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Email;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Email.Configuration;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Email;

public sealed class UserAccessEmailComposer(
    IOptions<EmailOptions> emailOptions,
    IHostEnvironment hostEnvironment) : IUserAccessEmailComposer
{
    private const string ProductName = "Employment Management System (EMS)";

    private readonly EmailOptions _emailOptions = emailOptions.Value;
    private readonly IHostEnvironment _hostEnvironment = hostEnvironment;

    public EmailMessage ComposeTemporaryPasswordInvite(TemporaryPasswordEmailModel model)
        => BuildTemporaryPasswordEmail(model, isResend: false);

    public EmailMessage ComposeTemporaryPasswordResend(TemporaryPasswordEmailModel model)
        => BuildTemporaryPasswordEmail(model, isResend: true);

    public EmailMessage ComposePasswordReset(PasswordResetEmailModel model)
    {
        var subject = "Reset your EMS password";
        var preheader = "Use the secure link below to reset your password within 1 hour.";
        var greetingName = GetGreetingName(model.Recipient);
        var supportEmail = model.SupportEmail ?? _emailOptions.SupportEmail;
        var expirationLabel = $"Expires in 1 hour ({FormatUtc(model.ExpiresAtUtc)})";
        var resetUrl = WebUtility.HtmlEncode(model.ResetUrl);

        var htmlBody = BuildShell(
            preheader: preheader,
            badgeText: "Password reset",
            headline: "Reset your EMS password",
            heroBody: $"{WebUtility.HtmlEncode(greetingName)}, we received a request to reset your EMS password. Use the secure button below to choose a new password before the link expires.",
            credentialBlockHtml: $$"""
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="width:100%; margin:24px 0 0 0;">
                  <tr>
                    <td style="padding:0 0 16px 0;">
                      {{BuildButton("Reset password", model.ResetUrl)}}
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:0 0 16px 0; font-size:13px; line-height:20px; color:#64748b;">
                      If the button does not work, copy and paste this URL into your browser:<br />
                      <a href="{{resetUrl}}" style="color:#0f766e; word-break:break-all;">{{resetUrl}}</a>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:16px; border:1px solid #fed7aa; background-color:#fff7ed; color:#b45309; font-size:14px; line-height:20px;">
                      {{WebUtility.HtmlEncode(expirationLabel)}}
                    </td>
                  </tr>
                </table>
                """,
            checklistHtml: """
                <tr>
                  <td style="padding:0 0 8px 0; font-size:16px; line-height:24px; color:#0f172a; font-weight:700;">
                    Next steps
                  </td>
                </tr>
                <tr>
                  <td style="padding:0 0 6px 0; font-size:15px; line-height:24px; color:#334155;">
                    1. Open the secure reset link above.
                  </td>
                </tr>
                <tr>
                  <td style="padding:0 0 6px 0; font-size:15px; line-height:24px; color:#334155;">
                    2. Choose a new password you have not used before.
                  </td>
                </tr>
                <tr>
                  <td style="padding:0; font-size:15px; line-height:24px; color:#334155;">
                    3. If you did not request this reset, ignore this email and contact support immediately.
                  </td>
                </tr>
                """,
            securityPanelHtml: BuildSecurityPanel(
                "Security notice",
                $"This message was sent from a no-reply mailbox. If you were not expecting it, contact {supportEmail} before using the reset link."),
            footerHtml: BuildFooter());

        var textBody = string.Join(
            Environment.NewLine,
            [
                ProductName,
                string.Empty,
                "Reset your EMS password",
                string.Empty,
                $"{greetingName}, we received a request to reset your EMS password.",
                $"Reset URL: {model.ResetUrl}",
                $"Expires: {FormatUtc(model.ExpiresAtUtc)} (valid for 1 hour)",
                $"If you did not request this reset, contact {supportEmail}.",
                "This message was sent from a no-reply mailbox."
            ]);

        return new EmailMessage(model.Recipient, subject, htmlBody, textBody);
    }

    private EmailMessage BuildTemporaryPasswordEmail(TemporaryPasswordEmailModel model, bool isResend)
    {
        var subject = isResend
            ? "Your EMS temporary password has been reissued"
            : "Your EMS account is ready";
        var preheader = isResend
            ? "A new temporary password has replaced the previous one and expires in 24 hours."
            : "Use your temporary password to sign in and change it within 24 hours.";
        var headline = isResend
            ? "Your temporary password was reissued"
            : "Your EMS account has been created";
        var badgeText = isResend ? "Password reissued" : "Temporary password";
        var greetingName = GetGreetingName(model.Recipient);
        var supportEmail = model.SupportEmail ?? _emailOptions.SupportEmail;
        var roleText = string.IsNullOrWhiteSpace(model.RoleName)
            ? "An administrator created access to EMS for you."
            : $"An administrator created access to EMS for you with the {WebUtility.HtmlEncode(model.RoleName)} role.";
        var issuedByText = string.IsNullOrWhiteSpace(model.IssuedByDisplayName)
            ? string.Empty
            : $" This temporary password was issued by {WebUtility.HtmlEncode(model.IssuedByDisplayName)}.";
        var expirationLabel = $"Expires in 24 hours ({FormatUtc(model.ExpiresAtUtc)})";
        var loginUrl = WebUtility.HtmlEncode(model.LoginUrl);
        var temporaryPassword = WebUtility.HtmlEncode(model.TemporaryPassword);
        var invalidationMessage = isResend
            ? "This new temporary password replaces all previous temporary passwords immediately."
            : "Change this temporary password immediately after your first sign-in.";

        var htmlBody = BuildShell(
            preheader: preheader,
            badgeText: badgeText,
            headline: headline,
            heroBody: $"{WebUtility.HtmlEncode(greetingName)}, {roleText}{issuedByText}",
            credentialBlockHtml: $$"""
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="width:100%; margin:24px 0 0 0;">
                  <tr>
                    <td style="padding:0 0 16px 0;">
                      {{BuildButton("Sign in to EMS", model.LoginUrl)}}
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:0 0 16px 0; font-size:13px; line-height:20px; color:#64748b;">
                      If the button does not work, copy and paste this URL into your browser:<br />
                      <a href="{{loginUrl}}" style="color:#0f766e; word-break:break-all;">{{loginUrl}}</a>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:16px; border:1px solid #d9e2ec; background-color:#f8fafc;">
                      <div style="font-size:13px; line-height:20px; color:#64748b; text-transform:uppercase; letter-spacing:0.08em;">Temporary password</div>
                      <div style="margin-top:8px; font-size:28px; line-height:32px; color:#111827; font-weight:700; letter-spacing:0.06em;">{{temporaryPassword}}</div>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:16px; border:1px solid #fed7aa; background-color:#fff7ed; color:#b45309; font-size:14px; line-height:20px;">
                      {{WebUtility.HtmlEncode(expirationLabel)}}
                    </td>
                  </tr>
                </table>
                """,
            checklistHtml: $$"""
                <tr>
                  <td style="padding:0 0 8px 0; font-size:16px; line-height:24px; color:#0f172a; font-weight:700;">
                    Next steps
                  </td>
                </tr>
                <tr>
                  <td style="padding:0 0 6px 0; font-size:15px; line-height:24px; color:#334155;">
                    1. Sign in using the button or URL above.
                  </td>
                </tr>
                <tr>
                  <td style="padding:0 0 6px 0; font-size:15px; line-height:24px; color:#334155;">
                    2. Enter the temporary password exactly as shown.
                  </td>
                </tr>
                <tr>
                  <td style="padding:0; font-size:15px; line-height:24px; color:#334155;">
                    3. {{WebUtility.HtmlEncode(invalidationMessage)}}
                  </td>
                </tr>
                """,
            securityPanelHtml: BuildSecurityPanel(
                "Security notice",
                $"Do not forward this email. If you were not expecting this message, contact {supportEmail} before using the temporary password."),
            footerHtml: BuildFooter());

        var textLines = new List<string>
        {
            ProductName,
            string.Empty,
            headline,
            string.Empty,
            $"{greetingName}, {WebUtility.HtmlDecode(roleText)}{WebUtility.HtmlDecode(issuedByText)}".Trim(),
            $"Sign in URL: {model.LoginUrl}",
            $"Temporary password: {model.TemporaryPassword}",
            $"Expires: {FormatUtc(model.ExpiresAtUtc)} (valid for 24 hours)",
            invalidationMessage,
            $"Support: {supportEmail}",
            "This message was sent from a no-reply mailbox."
        };

        return new EmailMessage(
            model.Recipient,
            subject,
            htmlBody,
            string.Join(Environment.NewLine, textLines));
    }

    private string BuildShell(
        string preheader,
        string badgeText,
        string headline,
        string heroBody,
        string credentialBlockHtml,
        string checklistHtml,
        string securityPanelHtml,
        string footerHtml)
    {
        var encodedPreheader = WebUtility.HtmlEncode(preheader);
        var encodedBadgeText = WebUtility.HtmlEncode(badgeText);
        var encodedHeadline = WebUtility.HtmlEncode(headline);
        var encodedHeroBody = WebUtility.HtmlEncode(heroBody);

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
              <body style="margin:0; padding:0; background-color:#f4f7fb; font-family:Segoe UI, Arial, Helvetica, sans-serif; color:#334155;">
                <div style="display:none; max-height:0; overflow:hidden; opacity:0; mso-hide:all;">
                  {{encodedPreheader}}
                </div>
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="width:100%; background-color:#f4f7fb; margin:0; padding:24px 0;">
                  <tr>
                    <td align="center">
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="width:100%; max-width:600px;">
                        <tr>
                          <td style="background-color:#0f766e; padding:16px 24px;">
                            <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                              <tr>
                                <td style="font-size:18px; line-height:24px; font-weight:700; color:#ffffff;">
                                  EMS
                                </td>
                                <td align="right">
                                  <span style="display:inline-block; padding:6px 10px; border-radius:999px; background-color:#dff6f2; color:#0f766e; font-size:12px; line-height:16px; font-weight:700; text-transform:uppercase; letter-spacing:0.06em;">
                                    {{encodedBadgeText}}
                                  </span>
                                </td>
                              </tr>
                            </table>
                          </td>
                        </tr>
                        <tr>
                          <td style="background-color:#ffffff; padding:32px 32px 24px 32px; border:1px solid #d9e2ec;">
                            <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                              <tr>
                                <td style="padding:0 0 12px 0; font-size:28px; line-height:34px; font-weight:700; color:#0f172a;">
                                  {{encodedHeadline}}
                                </td>
                              </tr>
                              <tr>
                                <td style="padding:0; font-size:16px; line-height:26px; color:#334155;">
                                  {{encodedHeroBody}}
                                </td>
                              </tr>
                              <tr>
                                <td>
                                  {{credentialBlockHtml}}
                                </td>
                              </tr>
                              <tr>
                                <td style="padding:24px 0 0 0;">
                                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="width:100%;">
                                    {{checklistHtml}}
                                  </table>
                                </td>
                              </tr>
                              <tr>
                                <td style="padding:24px 0 0 0;">
                                  {{securityPanelHtml}}
                                </td>
                              </tr>
                            </table>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:16px 24px 0 24px;">
                            {{footerHtml}}
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>
              </body>
            </html>
            """;
    }

    private string BuildButton(string label, string url)
    {
        var encodedLabel = WebUtility.HtmlEncode(label);
        var encodedUrl = WebUtility.HtmlEncode(url);

        return $$"""
            <table role="presentation" cellpadding="0" cellspacing="0">
              <tr>
                <td style="border-radius:8px; background-color:#0f766e;">
                  <a href="{{encodedUrl}}" style="display:inline-block; padding:14px 22px; color:#ffffff; text-decoration:none; font-size:15px; line-height:18px; font-weight:700;">
                    {{encodedLabel}}
                  </a>
                </td>
              </tr>
            </table>
            """;
    }

    private static string BuildSecurityPanel(string title, string body)
    {
        return $$"""
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="width:100%; border:1px solid #d9e2ec; background-color:#f8fafc;">
              <tr>
                <td style="padding:16px;">
                  <div style="font-size:15px; line-height:22px; color:#0f172a; font-weight:700;">
                    {{WebUtility.HtmlEncode(title)}}
                  </div>
                  <div style="margin-top:8px; font-size:14px; line-height:22px; color:#475569;">
                    {{WebUtility.HtmlEncode(body)}}
                  </div>
                </td>
              </tr>
            </table>
            """;
    }

    private string BuildFooter()
    {
        var environmentLabel = _hostEnvironment.IsProduction()
            ? string.Empty
            : $$"""
                <div style="margin-top:8px; font-size:12px; line-height:18px; color:#64748b;">
                  Environment: {{WebUtility.HtmlEncode(_hostEnvironment.EnvironmentName)}}
                </div>
                """;

        return $$"""
            <div style="font-size:13px; line-height:20px; color:#64748b;">
              {{ProductName}}<br />
              This is an automated message sent from a no-reply mailbox.
            </div>
            {{environmentLabel}}
            """;
    }

    private static string GetGreetingName(EmailRecipient recipient)
    {
        if (!string.IsNullOrWhiteSpace(recipient.DisplayName))
        {
            return recipient.DisplayName;
        }

        var atIndex = recipient.Address.IndexOf('@');
        return atIndex > 0
            ? recipient.Address[..atIndex]
            : recipient.Address;
    }

    private static string FormatUtc(DateTimeOffset value)
        => value
            .ToUniversalTime()
            .ToString("MMMM d, yyyy 'at' HH:mm 'UTC'", CultureInfo.InvariantCulture);
}
