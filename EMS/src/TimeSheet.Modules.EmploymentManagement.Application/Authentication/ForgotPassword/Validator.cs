using FluentValidation;

namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication.ForgotPassword;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
