namespace TimeSheet.Modules.EmploymentManagement.Application.Employees;

public interface IEmployeeReadService
{
    Task<PagedResult<EmployeeSummaryView>> ListAsync(EmployeeListRequest request, CancellationToken cancellationToken);

    Task<EmployeeDetailView?> GetAsync(Guid employeeId, CancellationToken cancellationToken);
}
