using FluentValidation.TestHelper;
using TimeSheet.Modules.EmploymentManagement.Application.Employees.Create;
using TimeSheet.Modules.EmploymentManagement.Domain.Employees;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests.Validation;

public sealed class CreateEmployeeCommandValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Validate_WhenEmployeeIsTooYoung_HasValidationError()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var command = new Command(
            "Ana",
            "Lopez",
            "ana.lopez@company.com",
            "5551234567",
            today.AddYears(-20),
            today,
            EmployeeStatusCodes.Active,
            null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Fact]
    public void Validate_WhenStatusIsUnknown_HasValidationError()
    {
        var command = new Command(
            "Ana",
            "Lopez",
            "ana.lopez@company.com",
            "5551234567",
            new DateOnly(1990, 6, 18),
            new DateOnly(2025, 1, 15),
            "Unknown",
            null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }
}
