using FluentValidation.TestHelper;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication.ForgotPassword;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests.Validation;

public sealed class ForgotPasswordCommandValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Should_Require_Valid_Email()
    {
        var result = _validator.TestValidate(new Command("not-an-email"));

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}
