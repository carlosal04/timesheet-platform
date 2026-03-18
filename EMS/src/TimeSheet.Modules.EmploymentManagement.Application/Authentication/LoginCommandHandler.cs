namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication;

public sealed class LoginCommandHandler
{
    private readonly IUserSessionAuthenticationService _authService;

    public LoginCommandHandler(IUserSessionAuthenticationService authService)
    {
        _authService = authService;
    }

    public Task<LoginResult> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        return _authService.LoginAsync(command.Email, command.Password, cancellationToken);
    }
}
