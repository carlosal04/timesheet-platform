using FluentValidation;

namespace TimeSheet.Modules.EmploymentManagement.Application.Addresses.List;

public sealed class Validator : AbstractValidator<Query>
{
    public Validator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty();
    }
}
