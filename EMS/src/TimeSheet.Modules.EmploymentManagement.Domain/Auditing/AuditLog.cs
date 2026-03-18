namespace TimeSheet.Modules.EmploymentManagement.Domain.Auditing;

public sealed class AuditLog
{
    public Guid Id { get; set; }

    public Guid? ActorUserId { get; set; }

    public Guid? SessionId { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    public string Result { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string? MetadataJson { get; set; }
}
