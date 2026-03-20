using Request = TimeSheet.Modules.EmploymentManagement.Application.Users.List.Request;
using Result = TimeSheet.Modules.EmploymentManagement.Application.Users.List.Result;

namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Users;

public interface IUserReadService
{
    Task<Result> ListAsync(Request request, CancellationToken cancellationToken);
}
