using FluentValidation;

namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication;

public sealed class LogoutCommandHandler
{
    private readonly IUserSessionAuthenticationService _authService;
    private readonly IValidator<LogoutCommand> _validator;

    public LogoutCommandHandler(IUserSessionAuthenticationService authService, IValidator<LogoutCommand> validator)
    {
        _authService = authService;
        _validator = validator;
    }

    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);
        await _authService.LogoutAsync(command.SessionId, cancellationToken);
    }
}
