using FluentValidation;

namespace TimeSheet.Modules.EmploymentManagement.Application.Employees;

public sealed class GetEmployeeByIdQueryHandler
{
    private readonly IEmployeeReadService _employeeReadService;
    private readonly IValidator<GetEmployeeByIdQuery> _validator;

    public GetEmployeeByIdQueryHandler(IEmployeeReadService employeeReadService, IValidator<GetEmployeeByIdQuery> validator)
    {
        _employeeReadService = employeeReadService;
        _validator = validator;
    }

    public async Task<GetEmployeeByIdResult> Handle(GetEmployeeByIdQuery query, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        var employee = await _employeeReadService.GetAsync(query.EmployeeId, cancellationToken);
        return new GetEmployeeByIdResult(employee);
    }
}
