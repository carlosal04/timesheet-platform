using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Employees;

namespace TimeSheet.Modules.EmploymentManagement.Application.Employees.Delete;

public sealed class Handler
{
    private readonly IEmployeeWriteService _employeeWriteService;
    private readonly IValidator<Command> _validator;

    public Handler(IEmployeeWriteService employeeWriteService, IValidator<Command> validator)
    {
        _employeeWriteService = employeeWriteService;
        _validator = validator;
    }

    public async Task Handle(Command command, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);
        await _employeeWriteService.DeleteAsync(command, cancellationToken);
    }
}
