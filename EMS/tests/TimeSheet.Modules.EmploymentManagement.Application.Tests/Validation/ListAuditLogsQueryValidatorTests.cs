using FluentValidation.TestHelper;
using TimeSheet.Modules.EmploymentManagement.Application.AuditLogs.List;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests.Validation;

public sealed class ListAuditLogsQueryValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Should_Reject_Invalid_ActionType()
    {
        var result = _validator.TestValidate(new Query(
            1,
            25,
            null,
            "NotCanonical",
            null,
            null,
            null,
            null,
            null));

        result.ShouldHaveValidationErrorFor(x => x.ActionType);
    }

    [Fact]
    public void Should_Accept_Canonical_ActionType()
    {
        var result = _validator.TestValidate(new Query(
            1,
            25,
            null,
            AuditActionTypes.AuditLogRead,
            null,
            null,
            null,
            null,
            null));

        result.ShouldNotHaveValidationErrorFor(x => x.ActionType);
    }
}
