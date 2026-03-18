using FluentValidation;

namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication.Logout;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
    }
}
