namespace TimeSheet.Modules.EmploymentManagement.Api.Contracts.Users;

public sealed record ResendTemporaryPasswordResponse(Guid UserId, DateTimeOffset TemporaryPasswordExpiresAtUtc);
