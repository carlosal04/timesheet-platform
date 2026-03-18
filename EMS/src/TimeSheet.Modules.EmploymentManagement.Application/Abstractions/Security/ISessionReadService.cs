using Result = TimeSheet.Modules.EmploymentManagement.Application.Authentication.GetSession.Result;

namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;

public interface ISessionReadService
{
    Task<Result?> GetAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
}
