namespace TimeSheet.Modules.EmploymentManagement.Application.AuditLogs.List;

public sealed record Query(
    int Page,
    int PageSize,
    Guid? ActorUserId,
    string? ActionType,
    string? EntityType,
    Guid? EntityId,
    string? Result,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc);
