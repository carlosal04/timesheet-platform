using FluentValidation.TestHelper;
using TimeSheet.Modules.EmploymentManagement.Application.Addresses.ListMine;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests.Validation;

public sealed class ListOwnAddressesQueryValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Validate_EmptyQuery_HasNoValidationErrors()
    {
        var result = _validator.TestValidate(new Query());

        result.ShouldNotHaveAnyValidationErrors();
    }
}
