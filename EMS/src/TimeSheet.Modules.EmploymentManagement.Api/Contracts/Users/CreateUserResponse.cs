namespace TimeSheet.Modules.EmploymentManagement.Api.Contracts.Users;

public sealed record CreateUserResponse(
    Guid UserId,
    string Email,
    Guid RoleId,
    string RoleCode,
    Guid? EmployeeId,
    bool MustChangePassword,
    DateTimeOffset TemporaryPasswordExpiresAtUtc);
