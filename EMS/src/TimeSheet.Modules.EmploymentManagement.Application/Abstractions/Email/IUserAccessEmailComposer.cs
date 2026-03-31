namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Email;

public interface IUserAccessEmailComposer
{
    EmailMessage ComposeTemporaryPasswordInvite(TemporaryPasswordEmailModel model);

    EmailMessage ComposeTemporaryPasswordResend(TemporaryPasswordEmailModel model);

    EmailMessage ComposePasswordReset(PasswordResetEmailModel model);
}
