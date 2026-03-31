namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication.GetSession;

public sealed record Result(
    Guid UserId,
    string Email,
    string RoleCode,
    Guid? EmployeeId,
    bool MustChangePassword,
    Guid SessionId,
    DateTimeOffset ExpiresAtUtc,
    int IdleTimeoutMinutes);
