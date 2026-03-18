namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication;

public sealed record LoginCommand(string Email, string Password);
