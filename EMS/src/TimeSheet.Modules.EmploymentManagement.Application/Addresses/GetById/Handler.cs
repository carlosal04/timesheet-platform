using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Addresses;

namespace TimeSheet.Modules.EmploymentManagement.Application.Addresses.GetById;

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
        var address = await _employeeAddressService.GetAsync(query, cancellationToken);
        return new Result(address);
    }
}
