using FluentValidation;

namespace TimeSheet.Modules.EmploymentManagement.Application.Addresses.Delete;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.AddressId).NotEmpty();
    }
}
