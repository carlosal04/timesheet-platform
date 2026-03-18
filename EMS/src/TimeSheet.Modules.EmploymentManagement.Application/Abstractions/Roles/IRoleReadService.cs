using Query = TimeSheet.Modules.EmploymentManagement.Application.Roles.List.Query;
using Result = TimeSheet.Modules.EmploymentManagement.Application.Roles.List.Result;

namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Roles;

public interface IRoleReadService
{
    Task<Result> ListAsync(Query query, CancellationToken cancellationToken);
}
