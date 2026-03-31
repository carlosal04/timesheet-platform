using FluentValidation.TestHelper;
using TimeSheet.Modules.EmploymentManagement.Application.Users.Create;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests.Validation;

public sealed class CreateUserCommandValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Should_Require_RoleId()
    {
        var result = _validator.TestValidate(new Command(Guid.Empty, null, "user@example.com"));

        result.ShouldHaveValidationErrorFor(x => x.RoleId);
    }

    [Fact]
    public void Should_Require_Email_When_EmployeeId_Is_Omitted()
    {
        var result = _validator.TestValidate(new Command(Guid.NewGuid(), null, string.Empty));

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Reject_Email_When_EmployeeId_Is_Provided()
    {
        var result = _validator.TestValidate(new Command(Guid.NewGuid(), Guid.NewGuid(), "user@example.com"));

        result.ShouldHaveValidationErrorFor(x => x);
    }
}
