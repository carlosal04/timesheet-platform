namespace TimeSheet.Modules.EmploymentManagement.Application.Employees;

public sealed class GetEmployeeByIdQueryHandler
{
    private readonly IEmployeeReadService _employeeReadService;

    public GetEmployeeByIdQueryHandler(IEmployeeReadService employeeReadService)
    {
        _employeeReadService = employeeReadService;
    }

    public async Task<GetEmployeeByIdResult> Handle(GetEmployeeByIdQuery query, CancellationToken cancellationToken)
    {
        var employee = await _employeeReadService.GetAsync(query.EmployeeId, cancellationToken);
        return new GetEmployeeByIdResult(employee);
    }
}
