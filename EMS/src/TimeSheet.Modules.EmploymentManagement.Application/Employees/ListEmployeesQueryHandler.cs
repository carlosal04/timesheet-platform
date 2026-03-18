using FluentValidation;

namespace TimeSheet.Modules.EmploymentManagement.Application.Employees;

public sealed class ListEmployeesQueryHandler
{
    private readonly IEmployeeReadService _employeeReadService;
    private readonly IValidator<ListEmployeesQuery> _validator;

    public ListEmployeesQueryHandler(IEmployeeReadService employeeReadService, IValidator<ListEmployeesQuery> validator)
    {
        _employeeReadService = employeeReadService;
        _validator = validator;
    }

    public async Task<PagedResult<EmployeeSummaryView>> Handle(ListEmployeesQuery query, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        return await _employeeReadService.ListAsync(
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
