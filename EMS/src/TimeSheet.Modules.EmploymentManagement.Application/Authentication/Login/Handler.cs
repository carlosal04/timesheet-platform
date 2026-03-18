using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;

namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication.Login;

public sealed class Handler
{
    private readonly IUserSessionAuthenticationService _authService;
    private readonly IValidator<Command> _validator;

    public Handler(IUserSessionAuthenticationService authService, IValidator<Command> validator)
    {
        _authService = authService;
        _validator = validator;
    }

    public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);
        return await _authService.LoginAsync(command.Email, command.Password, cancellationToken);
    }
}
