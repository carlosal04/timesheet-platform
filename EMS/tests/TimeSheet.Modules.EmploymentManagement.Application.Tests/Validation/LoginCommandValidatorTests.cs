using FluentValidation.TestHelper;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication.Login;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests.Validation;

public sealed class LoginCommandValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Should_Require_Email_And_Password()
    {
        var result = _validator.TestValidate(new Command(string.Empty, string.Empty));

        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Reject_Invalid_Email_Format()
    {
        var result = _validator.TestValidate(new Command("not-an-email", "secret"));

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}
