namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Email;

public sealed record EmailMessage(
    EmailRecipient To,
    string Subject,
    string HtmlBody,
    string TextBody);
