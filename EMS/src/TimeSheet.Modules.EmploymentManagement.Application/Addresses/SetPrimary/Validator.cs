using FluentValidation;

namespace TimeSheet.Modules.EmploymentManagement.Application.Addresses.SetPrimary;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.AddressId).NotEmpty();
    }
}
