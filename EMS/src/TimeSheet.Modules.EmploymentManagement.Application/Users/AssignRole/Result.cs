namespace TimeSheet.Modules.EmploymentManagement.Application.Users.AssignRole;

public sealed record Result(Guid UserId, Guid RoleId, string RoleCode, int SessionsRevoked);
