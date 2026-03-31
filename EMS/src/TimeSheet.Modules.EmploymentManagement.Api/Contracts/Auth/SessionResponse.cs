namespace TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;

public sealed record SessionResponse(
    Guid UserId,
    string Email,
    string RoleCode,
    Guid? EmployeeId,
    bool MustChangePassword,
    Guid SessionId,
    DateTimeOffset ExpiresAtUtc,
    int IdleTimeoutMinutes);
