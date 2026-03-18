using FluentValidation;

namespace TimeSheet.Modules.EmploymentManagement.Application.Addresses.DeleteOwn;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.AddressId).NotEmpty();
    }
}
