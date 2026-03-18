using Command = TimeSheet.Modules.EmploymentManagement.Application.Users.AssignRole.Command;
using Result = TimeSheet.Modules.EmploymentManagement.Application.Users.AssignRole.Result;

namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Users;

public interface IUserRoleService
{
    Task<Result> AssignRoleAsync(Command command, CancellationToken cancellationToken);
}
