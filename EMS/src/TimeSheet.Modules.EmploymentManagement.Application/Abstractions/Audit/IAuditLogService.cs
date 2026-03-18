using TimeSheet.Modules.EmploymentManagement.Application.AuditLogs.List;

namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Audit;

public interface IAuditLogService
{
    Task WriteAsync(AuditWriteEntry entry, CancellationToken cancellationToken);

    Task<Result> ListAsync(Query query, CancellationToken cancellationToken);
}
