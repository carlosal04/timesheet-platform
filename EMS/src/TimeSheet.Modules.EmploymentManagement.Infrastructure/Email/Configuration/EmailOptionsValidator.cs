using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Email.Configuration;

public sealed class EmailOptionsValidator : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.FromAddress))
        {
            failures.Add("Email:FromAddress is required.");
        }
        else if (!MailAddress.TryCreate(options.FromAddress, out _))
        {
            failures.Add("Email:FromAddress must be a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(options.SupportEmail))
        {
            failures.Add("Email:SupportEmail is required.");
        }
        else if (!MailAddress.TryCreate(options.SupportEmail, out _))
        {
            failures.Add("Email:SupportEmail must be a valid email address.");
        }

        if (options.Smtp is null)
        {
            failures.Add("Email:Smtp configuration is required.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(options.Smtp.Host))
            {
                failures.Add("Email:Smtp:Host is required.");
            }

            if (options.Smtp.Port is < 1 or > 65535)
            {
                failures.Add("Email:Smtp:Port must be between 1 and 65535.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
