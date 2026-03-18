using FluentValidation;

namespace TimeSheet.Modules.EmploymentManagement.Application.Employees.Delete;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty();
    }
}
