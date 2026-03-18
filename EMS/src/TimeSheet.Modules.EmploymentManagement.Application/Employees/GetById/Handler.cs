using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Employees;

namespace TimeSheet.Modules.EmploymentManagement.Application.Employees.GetById;

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

        var employee = await _employeeReadService.GetAsync(query.EmployeeId, cancellationToken);
        return new Result(employee);
    }
}
