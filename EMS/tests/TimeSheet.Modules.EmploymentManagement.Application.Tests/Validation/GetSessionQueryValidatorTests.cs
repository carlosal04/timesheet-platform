using FluentValidation.TestHelper;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication.GetSession;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests.Validation;

public sealed class GetSessionQueryValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Validate_EmptyQuery_HasNoValidationErrors()
    {
        var result = _validator.TestValidate(new Query());

        result.ShouldNotHaveAnyValidationErrors();
    }
}
