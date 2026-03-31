namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Email;

public sealed record EmailRecipient(string Address, string? DisplayName = null);
