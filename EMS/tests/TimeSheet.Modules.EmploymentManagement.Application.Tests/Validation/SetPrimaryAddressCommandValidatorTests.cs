using FluentValidation.TestHelper;
using TimeSheet.Modules.EmploymentManagement.Application.Addresses.SetPrimary;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests.Validation;

public sealed class SetPrimaryAddressCommandValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Validate_WhenEmployeeIdIsEmpty_HasValidationError()
    {
        var command = new Command(Guid.Empty, Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.EmployeeId);
    }

    [Fact]
    public void Validate_WhenAddressIdIsEmpty_HasValidationError()
    {
        var command = new Command(Guid.NewGuid(), Guid.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AddressId);
    }
}
