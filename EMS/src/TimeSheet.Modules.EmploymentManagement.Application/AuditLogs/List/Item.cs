namespace TimeSheet.Modules.EmploymentManagement.Application.AuditLogs.List;

public sealed record Item(
    Guid Id,
    Guid? ActorUserId,
    Guid? SessionId,
    string ActionType,
    string EntityType,
    Guid? EntityId,
    string Result,
    string CorrelationId,
    DateTimeOffset OccurredAtUtc);
