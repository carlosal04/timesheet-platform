using FluentValidation.TestHelper;
using TimeSheet.Modules.EmploymentManagement.Application.Users.ResendTemporaryPassword;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests.Validation;

public sealed class ResendTemporaryPasswordCommandValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Should_Require_UserId()
    {
        var result = _validator.TestValidate(new Command(Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
