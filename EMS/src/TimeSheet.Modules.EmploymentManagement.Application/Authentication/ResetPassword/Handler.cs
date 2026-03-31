using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;

namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication.ResetPassword;

public sealed class Handler
{
    private readonly IPasswordResetService _passwordResetService;
    private readonly IValidator<Command> _validator;

    public Handler(IPasswordResetService passwordResetService, IValidator<Command> validator)
    {
        _passwordResetService = passwordResetService;
        _validator = validator;
    }

    public async Task<bool> Handle(Command command, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);
        return await _passwordResetService.ResetAsync(command.Token, command.NewPassword, cancellationToken);
    }
}
