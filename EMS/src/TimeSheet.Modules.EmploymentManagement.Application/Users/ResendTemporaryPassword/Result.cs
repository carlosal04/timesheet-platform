namespace TimeSheet.Modules.EmploymentManagement.Application.Users.ResendTemporaryPassword;

public sealed record Result(Guid UserId, DateTimeOffset TemporaryPasswordExpiresAtUtc);
