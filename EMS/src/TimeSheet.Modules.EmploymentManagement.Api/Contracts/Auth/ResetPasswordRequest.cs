namespace TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;

public sealed record ResetPasswordRequest(string Token, string NewPassword);
