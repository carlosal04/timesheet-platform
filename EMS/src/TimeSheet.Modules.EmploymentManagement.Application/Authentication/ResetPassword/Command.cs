namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication.ResetPassword;

public sealed record Command(string Token, string NewPassword);
