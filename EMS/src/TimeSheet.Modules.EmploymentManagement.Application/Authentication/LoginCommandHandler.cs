using FluentValidation;

namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication;

public sealed class LoginCommandHandler
{
    private readonly IUserSessionAuthenticationService _authService;
    private readonly IValidator<LoginCommand> _validator;

    public LoginCommandHandler(IUserSessionAuthenticationService authService, IValidator<LoginCommand> validator)
    {
        _authService = authService;
        _validator = validator;
    }

    public async Task<LoginResult> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);
        return await _authService.LoginAsync(command.Email, command.Password, cancellationToken);
    }
}
