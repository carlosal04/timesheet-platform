using FluentValidation.TestHelper;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication.ChangePassword;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests.Validation;

public sealed class ChangePasswordCommandValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Should_Require_Current_And_New_Password()
    {
        var result = _validator.TestValidate(new Command(string.Empty, string.Empty));

        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Should_Reject_Reused_Password()
    {
        var result = _validator.TestValidate(new Command("P@ssw0rd123!", "P@ssw0rd123!"));

        result.ShouldHaveValidationErrorFor(x => x);
    }
}
