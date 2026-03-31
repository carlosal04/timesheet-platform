namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Email;

public sealed record TemporaryPasswordEmailModel(
    EmailRecipient Recipient,
    string LoginUrl,
    string TemporaryPassword,
    DateTimeOffset ExpiresAtUtc,
    string? RoleName = null,
    string? SupportEmail = null,
    string? IssuedByDisplayName = null);
