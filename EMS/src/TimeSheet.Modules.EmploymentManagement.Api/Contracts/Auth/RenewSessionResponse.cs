namespace TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;

public sealed record RenewSessionResponse(Guid SessionId, DateTimeOffset ExpiresAtUtc, int IdleTimeoutMinutes);
