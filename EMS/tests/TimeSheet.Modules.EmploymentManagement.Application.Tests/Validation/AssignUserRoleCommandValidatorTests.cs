using FluentValidation.TestHelper;
using TimeSheet.Modules.EmploymentManagement.Application.Users.AssignRole;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests.Validation;

public sealed class AssignUserRoleCommandValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Validate_WhenUserIdIsEmpty_HasValidationError()
    {
        var result = _validator.TestValidate(new Command(Guid.Empty, Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WhenRoleIdIsEmpty_HasValidationError()
    {
        var result = _validator.TestValidate(new Command(Guid.NewGuid(), Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.RoleId);
    }
}
