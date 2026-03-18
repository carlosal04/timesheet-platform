using FluentValidation.TestHelper;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication.Renew;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests.Validation;

public sealed class RenewCommandValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Validate_EmptyCommand_HasNoValidationErrors()
    {
        var result = _validator.TestValidate(new Command());

        result.ShouldNotHaveAnyValidationErrors();
    }
}
