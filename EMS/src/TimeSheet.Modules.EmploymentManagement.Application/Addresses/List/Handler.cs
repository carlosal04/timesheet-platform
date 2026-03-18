using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Addresses;

namespace TimeSheet.Modules.EmploymentManagement.Application.Addresses.List;

public sealed class Handler
{
    private readonly IEmployeeAddressService _employeeAddressService;
    private readonly IValidator<Query> _validator;

    public Handler(IEmployeeAddressService employeeAddressService, IValidator<Query> validator)
    {
        _employeeAddressService = employeeAddressService;
        _validator = validator;
    }

    public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(query, cancellationToken);
        return await _employeeAddressService.ListAsync(query, cancellationToken);
    }
}
