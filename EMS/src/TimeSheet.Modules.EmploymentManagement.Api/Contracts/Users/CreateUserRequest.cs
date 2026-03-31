namespace TimeSheet.Modules.EmploymentManagement.Api.Contracts.Users;

public sealed record CreateUserRequest(Guid RoleId, Guid? EmployeeId, string? Email);
