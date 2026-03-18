using FluentValidation.TestHelper;
using TimeSheet.Modules.EmploymentManagement.Application.Employees.List;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests.Validation;

public sealed class ListEmployeesQueryValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Should_Reject_Invalid_Paging_And_Status()
    {
        var result = _validator.TestValidate(new Query(
            0,
            101,
            null,
            "Unknown",
            null,
            null,
            false,
            false));

        result.ShouldHaveValidationErrorFor(x => x.Page);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Should_Reject_Invalid_HireDate_Range()
    {
        var result = _validator.TestValidate(new Query(
            1,
            25,
            null,
            null,
            new DateOnly(2025, 2, 1),
            new DateOnly(2025, 1, 1),
            false,
            false));

        result.ShouldHaveValidationErrorFor(x => x);
    }
}
