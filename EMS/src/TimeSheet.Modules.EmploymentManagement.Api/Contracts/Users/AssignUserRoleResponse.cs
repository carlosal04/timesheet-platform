namespace TimeSheet.Modules.EmploymentManagement.Api.Contracts.Users;

public sealed record AssignUserRoleResponse(Guid UserId, Guid RoleId, string RoleCode, int SessionsRevoked);
