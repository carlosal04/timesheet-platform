using FluentValidation.TestHelper;
using TimeSheet.Modules.EmploymentManagement.Application.Addresses.SetOwnPrimary;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests.Validation;

public sealed class SetOwnPrimaryAddressCommandValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Validate_WhenAddressIdIsEmpty_HasValidationError()
    {
        var command = new Command(Guid.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AddressId);
    }
}
