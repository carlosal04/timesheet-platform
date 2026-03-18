using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Employees;

namespace TimeSheet.Modules.EmploymentManagement.Application.Employees.Update;

public sealed class Handler
{
    private readonly IEmployeeWriteService _employeeWriteService;
    private readonly IValidator<Command> _validator;

    public Handler(IEmployeeWriteService employeeWriteService, IValidator<Command> validator)
    {
        _employeeWriteService = employeeWriteService;
        _validator = validator;
    }

    public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);
        return await _employeeWriteService.UpdateAsync(command, cancellationToken);
    }
}
