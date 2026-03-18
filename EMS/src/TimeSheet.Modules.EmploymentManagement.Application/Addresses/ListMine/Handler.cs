using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Addresses;
using ListResult = TimeSheet.Modules.EmploymentManagement.Application.Addresses.List.Result;

namespace TimeSheet.Modules.EmploymentManagement.Application.Addresses.ListMine;

public sealed class Handler
{
    private readonly IEmployeeAddressService _employeeAddressService;
    private readonly IValidator<Query> _validator;

    public Handler(IEmployeeAddressService employeeAddressService, IValidator<Query> validator)
    {
        _employeeAddressService = employeeAddressService;
        _validator = validator;
    }

    public async Task<ListResult> Handle(Query query, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(query, cancellationToken);
        return await _employeeAddressService.ListOwnAsync(query, cancellationToken);
    }
}
