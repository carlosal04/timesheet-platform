using FluentValidation;

namespace TimeSheet.Modules.EmploymentManagement.Application.Users.Create;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty();

        RuleFor(x => x)
            .Must(x => !(x.EmployeeId.HasValue && !string.IsNullOrWhiteSpace(x.Email)))
            .WithMessage("email must be omitted when employeeId is provided.");

        When(x => !x.EmployeeId.HasValue, () =>
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();
        });
    }
}
