namespace TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;

public sealed record LoginResponse(Guid UserId, string Email, string RoleCode, bool MustChangePassword);
