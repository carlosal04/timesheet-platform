using FluentValidation;

namespace TimeSheet.Modules.EmploymentManagement.Application.Addresses.GetById;

public sealed class Validator : AbstractValidator<Query>
{
    public Validator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.AddressId).NotEmpty();
    }
}
