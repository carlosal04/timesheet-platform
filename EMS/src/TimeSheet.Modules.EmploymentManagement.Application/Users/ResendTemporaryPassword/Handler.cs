using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Users;

namespace TimeSheet.Modules.EmploymentManagement.Application.Users.ResendTemporaryPassword;

public sealed class Handler
{
    private readonly IUserProvisioningService _userProvisioningService;
    private readonly IValidator<Command> _validator;

    public Handler(IUserProvisioningService userProvisioningService, IValidator<Command> validator)
    {
        _userProvisioningService = userProvisioningService;
        _validator = validator;
    }

    public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);
        return await _userProvisioningService.ResendTemporaryPasswordAsync(command.UserId, cancellationToken);
    }
}
