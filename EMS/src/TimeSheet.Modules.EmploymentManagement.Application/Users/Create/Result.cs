namespace TimeSheet.Modules.EmploymentManagement.Application.Users.Create;

public sealed record Result(
    Guid UserId,
    string Email,
    Guid RoleId,
    string RoleCode,
    Guid? EmployeeId,
    bool MustChangePassword,
    DateTimeOffset TemporaryPasswordExpiresAtUtc);
