using FluentValidation;

namespace TimeSheet.Modules.EmploymentManagement.Application.Users.ResendTemporaryPassword;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
