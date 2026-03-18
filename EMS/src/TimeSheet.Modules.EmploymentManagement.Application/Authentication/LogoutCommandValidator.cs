using FluentValidation;

namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
    }
}
