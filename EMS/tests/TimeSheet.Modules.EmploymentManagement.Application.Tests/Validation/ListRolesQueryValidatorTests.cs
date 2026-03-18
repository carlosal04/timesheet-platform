using FluentValidation.TestHelper;
using TimeSheet.Modules.EmploymentManagement.Application.Roles.List;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests.Validation;

public sealed class ListRolesQueryValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Validate_EmptyQuery_HasNoValidationErrors()
    {
        var result = _validator.TestValidate(new Query(false));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
