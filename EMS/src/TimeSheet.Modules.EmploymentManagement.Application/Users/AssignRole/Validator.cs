using FluentValidation;

namespace TimeSheet.Modules.EmploymentManagement.Application.Users.AssignRole;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleId).NotEmpty();
    }
}
