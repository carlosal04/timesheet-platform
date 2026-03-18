using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Users;

namespace TimeSheet.Modules.EmploymentManagement.Application.Users.AssignRole;

public sealed class Handler
{
    private readonly IUserRoleService _userRoleService;
    private readonly IValidator<Command> _validator;

    public Handler(IUserRoleService userRoleService, IValidator<Command> validator)
    {
        _userRoleService = userRoleService;
        _validator = validator;
    }

    public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);
        return await _userRoleService.AssignRoleAsync(command, cancellationToken);
    }
}
