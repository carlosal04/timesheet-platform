using Command = TimeSheet.Modules.EmploymentManagement.Application.Users.Create.Command;
using Result = TimeSheet.Modules.EmploymentManagement.Application.Users.Create.Result;
using ResendResult = TimeSheet.Modules.EmploymentManagement.Application.Users.ResendTemporaryPassword.Result;

namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Users;

public interface IUserProvisioningService
{
    Task<Result> CreateAsync(Command command, CancellationToken cancellationToken);

    Task<ResendResult> ResendTemporaryPasswordAsync(Guid userId, CancellationToken cancellationToken);
}
