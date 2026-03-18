namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication.Renew;

public sealed record Result(Guid SessionId, DateTimeOffset ExpiresAtUtc, int IdleTimeoutMinutes);
