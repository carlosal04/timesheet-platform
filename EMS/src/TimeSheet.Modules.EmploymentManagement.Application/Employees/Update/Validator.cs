using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Domain.Employees;

namespace TimeSheet.Modules.EmploymentManagement.Application.Employees.Update;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty();

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Phone)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.Status)
            .Must(value => value is EmployeeStatusCodes.Active or EmployeeStatusCodes.Inactive)
            .WithMessage("status must be Active or Inactive.");

        RuleFor(x => x.DateOfBirth)
            .Must(BeAtLeastTwentyOneYearsOld)
            .WithMessage("dateOfBirth must represent an employee who is 21 years old or older on the current date.");

        RuleFor(x => x)
            .Must(x => x.HireDate >= x.DateOfBirth.AddYears(14))
            .WithMessage("hireDate must not be earlier than dateOfBirth + 14 years.");
    }

    private static bool BeAtLeastTwentyOneYearsOld(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        return dateOfBirth.AddYears(21) <= today;
    }
}
