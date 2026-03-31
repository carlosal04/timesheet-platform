namespace TimeSheet.Modules.EmploymentManagement.Application.Users.List;

public sealed record Item(
    Guid Id,
    string Email,
    Guid RoleId,
    string RoleCode,
    string RoleName,
    Guid? EmployeeId,
    string? EmployeeName,
    bool IsActive,
    bool MustChangePassword,
    DateTimeOffset? TemporaryPasswordExpiresAtUtc,
    DateTimeOffset? LastTemporaryPasswordIssuedAtUtc);
