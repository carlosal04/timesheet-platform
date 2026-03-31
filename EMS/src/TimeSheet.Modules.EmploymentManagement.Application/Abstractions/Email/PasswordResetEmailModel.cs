namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Email;

public sealed record PasswordResetEmailModel(
    EmailRecipient Recipient,
    string ResetUrl,
    DateTimeOffset ExpiresAtUtc,
    string? SupportEmail = null);
