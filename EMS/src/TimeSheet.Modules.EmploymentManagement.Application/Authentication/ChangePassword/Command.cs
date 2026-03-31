namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication.ChangePassword;

public sealed record Command(string CurrentPassword, string NewPassword);
