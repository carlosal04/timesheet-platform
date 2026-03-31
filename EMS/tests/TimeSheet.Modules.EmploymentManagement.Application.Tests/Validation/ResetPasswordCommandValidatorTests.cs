using FluentValidation.TestHelper;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication.ResetPassword;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests.Validation;

public sealed class ResetPasswordCommandValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Should_Require_Token_And_NewPassword()
    {
        var result = _validator.TestValidate(new Command(string.Empty, string.Empty));

        result.ShouldHaveValidationErrorFor(x => x.Token);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
