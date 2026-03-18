using FluentValidation;

namespace TimeSheet.Modules.EmploymentManagement.Application.Employees.GetById;

public sealed class Validator : AbstractValidator<Query>
{
    public Validator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}
