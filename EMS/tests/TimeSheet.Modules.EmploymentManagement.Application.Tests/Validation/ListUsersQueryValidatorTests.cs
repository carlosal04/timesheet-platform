using FluentValidation.TestHelper;
using TimeSheet.Modules.EmploymentManagement.Application.Users.List;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests.Validation;

public sealed class ListUsersQueryValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Should_Reject_Invalid_Paging()
    {
        var result = _validator.TestValidate(new Query(0, 101, null, null, false));

        result.ShouldHaveValidationErrorFor(x => x.Page);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Validate_EmptyQuery_HasNoValidationErrors()
    {
        var result = _validator.TestValidate(new Query(1, 25, null, null, false));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
