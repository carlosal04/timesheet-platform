using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;

namespace TimeSheet.Modules.EmploymentManagement.Application.AuditLogs.List;

public sealed class Validator : AbstractValidator<Query>
{
    public Validator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.EntityType)
            .Must(value => string.IsNullOrWhiteSpace(value) || IsValidEntityType(value))
            .WithMessage("entityType must be a canonical audit entity type when provided.");

        RuleFor(x => x.Result)
            .Must(value => string.IsNullOrWhiteSpace(value) || IsValidResult(value))
            .WithMessage("result must be a canonical audit result when provided.");

        RuleFor(x => x)
            .Must(x => !x.FromUtc.HasValue || !x.ToUtc.HasValue || x.FromUtc.Value <= x.ToUtc.Value)
            .WithMessage("fromUtc must be earlier than or equal to toUtc.");
    }

    private static bool IsValidEntityType(string value)
    {
        return value is AuditEntityTypes.Authentication
            or AuditEntityTypes.UserSession
            or AuditEntityTypes.User
            or AuditEntityTypes.Employee
            or AuditEntityTypes.EmployeeAddress
            or AuditEntityTypes.AuditLog
            or AuditEntityTypes.System;
    }

    private static bool IsValidResult(string value)
    {
        return value is AuditResults.Success
            or AuditResults.Failure
            or AuditResults.Denied
            or AuditResults.Rejected
            or AuditResults.NotFound
            or AuditResults.Conflict;
    }
}
