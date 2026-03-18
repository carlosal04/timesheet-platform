namespace TimeSheet.Modules.EmploymentManagement.Application.Employees;

public sealed class ListEmployeesQueryHandler
{
    private readonly IEmployeeReadService _employeeReadService;

    public ListEmployeesQueryHandler(IEmployeeReadService employeeReadService)
    {
        _employeeReadService = employeeReadService;
    }

    public Task<PagedResult<EmployeeSummaryView>> Handle(ListEmployeesQuery query, CancellationToken cancellationToken)
    {
        return _employeeReadService.ListAsync(
            new EmployeeListRequest(
                query.Page,
                query.PageSize,
                query.Name,
                query.Status,
                query.HireDateFrom,
                query.HireDateTo,
                query.IncludeDeleted,
                query.IncludePrimaryAddress),
            cancellationToken);
    }
}
