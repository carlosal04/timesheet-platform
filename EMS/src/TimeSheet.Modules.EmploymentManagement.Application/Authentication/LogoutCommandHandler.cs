namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication;

public sealed class LogoutCommandHandler
{
    private readonly IUserSessionAuthenticationService _authService;

    public LogoutCommandHandler(IUserSessionAuthenticationService authService)
    {
        _authService = authService;
    }

    public Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        return _authService.LogoutAsync(command.SessionId, cancellationToken);
    }
}
