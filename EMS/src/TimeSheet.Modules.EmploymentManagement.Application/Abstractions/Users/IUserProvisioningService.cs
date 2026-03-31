using Command = TimeSheet.Modules.EmploymentManagement.Application.Users.Create.Command;
using Result = TimeSheet.Modules.EmploymentManagement.Application.Users.Create.Result;

namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Users;

public interface IUserProvisioningService
{
    Task<Result> CreateAsync(Command command, CancellationToken cancellationToken);
}
