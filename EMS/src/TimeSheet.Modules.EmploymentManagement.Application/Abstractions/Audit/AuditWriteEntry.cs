namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Audit;

public sealed record AuditWriteEntry(
    string ActionType,
    string EntityType,
    string Result,
    Guid? EntityId = null,
    object? Metadata = null,
    Guid? ActorUserId = null,
    Guid? SessionId = null,
    string? CorrelationId = null);
