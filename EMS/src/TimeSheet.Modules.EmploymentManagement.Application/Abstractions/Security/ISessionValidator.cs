namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;

public interface ISessionValidator
{
    Task<SessionValidationResult> ValidateAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
}
