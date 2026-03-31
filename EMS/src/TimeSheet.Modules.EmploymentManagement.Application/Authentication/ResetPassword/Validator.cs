using FluentValidation;

namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication.ResetPassword;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.Token)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty();
    }
}
