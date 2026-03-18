using FluentValidation.TestHelper;
using TimeSheet.Modules.EmploymentManagement.Application.Addresses.Create;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests.Validation;

public sealed class CreateAddressCommandValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Validate_WhenAddressTypeIsUnknown_HasValidationError()
    {
        var command = new Command(
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

    [Fact]
    public void Validate_WhenCountryCodeIsWrongLength_HasValidationError()
    {
        var command = new Command(
            Guid.NewGuid(),
            "Home",
            false,
            "100 Main St",
            null,
            "Pittsburgh",
            "PA",
            "15222",
            "USA");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CountryCode);
    }
}
