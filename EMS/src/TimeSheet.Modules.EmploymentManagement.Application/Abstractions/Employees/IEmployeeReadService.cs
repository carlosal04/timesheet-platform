using EmployeeDetail = TimeSheet.Modules.EmploymentManagement.Application.Employees.GetById.Employee;
using ListRequest = TimeSheet.Modules.EmploymentManagement.Application.Employees.List.Request;
using ListResult = TimeSheet.Modules.EmploymentManagement.Application.Employees.List.Result;

namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Employees;

public interface IEmployeeReadService
{
    Task<ListResult> ListAsync(ListRequest request, CancellationToken cancellationToken);

    Task<EmployeeDetail?> GetAsync(Guid employeeId, CancellationToken cancellationToken);
}
