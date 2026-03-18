using FluentValidation.TestHelper;
using TimeSheet.Modules.EmploymentManagement.Application.Addresses.Update;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests.Validation;

public sealed class UpdateAddressCommandValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Validate_WhenAddressIdIsEmpty_HasValidationError()
    {
        var command = new Command(
            Guid.NewGuid(),
            Guid.Empty,
            "Home",
            false,
            "100 Main St",
            null,
            "Pittsburgh",
            "PA",
            "15222",
            "US");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AddressId);
    }

    [Fact]
    public void Validate_WhenAddressTypeIsUnknown_HasValidationError()
    {
        var command = new Command(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Unknown",
            true,
            "100 Main St",
            null,
            "Pittsburgh",
            "PA",
            "15222",
            "US");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AddressType);
    }
}
