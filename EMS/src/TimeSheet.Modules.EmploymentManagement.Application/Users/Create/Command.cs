namespace TimeSheet.Modules.EmploymentManagement.Application.Users.Create;

public sealed record Command(Guid RoleId, Guid? EmployeeId, string? Email);
