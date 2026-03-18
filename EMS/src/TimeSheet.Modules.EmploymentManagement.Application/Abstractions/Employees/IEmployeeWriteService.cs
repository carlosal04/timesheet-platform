using CreateCommand = TimeSheet.Modules.EmploymentManagement.Application.Employees.Create.Command;
using CreateResult = TimeSheet.Modules.EmploymentManagement.Application.Employees.Create.Result;
using DeleteCommand = TimeSheet.Modules.EmploymentManagement.Application.Employees.Delete.Command;
using UpdateCommand = TimeSheet.Modules.EmploymentManagement.Application.Employees.Update.Command;
using UpdateResult = TimeSheet.Modules.EmploymentManagement.Application.Employees.Update.Result;

namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Employees;

public interface IEmployeeWriteService
{
    Task<CreateResult> CreateAsync(CreateCommand command, CancellationToken cancellationToken);

    Task<UpdateResult> UpdateAsync(UpdateCommand command, CancellationToken cancellationToken);

    Task DeleteAsync(DeleteCommand command, CancellationToken cancellationToken);
}
