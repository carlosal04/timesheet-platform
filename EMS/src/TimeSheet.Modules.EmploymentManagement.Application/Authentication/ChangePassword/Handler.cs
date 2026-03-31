using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;

namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication.ChangePassword;

public sealed class Handler
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUserSessionAuthenticationService _authService;
    private readonly IValidator<Command> _validator;

    public Handler(
        ICurrentUserContext currentUserContext,
        IUserSessionAuthenticationService authService,
        IValidator<Command> validator)
    {
        _currentUserContext = currentUserContext;
        _authService = authService;
        _validator = validator;
    }

    public async Task<Result?> Handle(Command command, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        if (!_currentUserContext.IsAuthenticated
            || !_currentUserContext.UserId.HasValue
            || !_currentUserContext.SessionId.HasValue)
        {
            return null;
        }

        return await _authService.ChangePasswordAsync(
            _currentUserContext.UserId.Value,
            _currentUserContext.SessionId.Value,
            command.CurrentPassword,
            command.NewPassword,
            cancellationToken);
    }
}
