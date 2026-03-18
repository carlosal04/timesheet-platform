namespace TimeSheet.Modules.EmploymentManagement.Application.Users.AssignRole;

public sealed record Command(Guid UserId, Guid RoleId);
