using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Employees;

namespace TimeSheet.Modules.EmploymentManagement.Application.Employees.List;

public sealed class Handler
{
    private readonly IEmployeeReadService _employeeReadService;
    private readonly IValidator<Query> _validator;

    public Handler(IEmployeeReadService employeeReadService, IValidator<Query> validator)
    {
        _employeeReadService = employeeReadService;
        _validator = validator;
    }

    public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        return await _employeeReadService.ListAsync(
            new Request(
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
