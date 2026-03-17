namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication;

public interface ISessionValidator
{
    Task<SessionValidationResult> ValidateAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
}
