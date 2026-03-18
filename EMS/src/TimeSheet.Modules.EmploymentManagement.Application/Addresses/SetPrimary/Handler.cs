using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Addresses;

namespace TimeSheet.Modules.EmploymentManagement.Application.Addresses.SetPrimary;

public sealed class Handler
{
    private readonly IEmployeeAddressService _employeeAddressService;
    private readonly IValidator<Command> _validator;

    public Handler(IEmployeeAddressService employeeAddressService, IValidator<Command> validator)
    {
        _employeeAddressService = employeeAddressService;
        _validator = validator;
    }

    public async Task Handle(Command command, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);
        await _employeeAddressService.SetPrimaryAsync(command, cancellationToken);
    }
}
