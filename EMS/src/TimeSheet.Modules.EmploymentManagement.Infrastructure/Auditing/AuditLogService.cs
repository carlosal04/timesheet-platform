using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Audit;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Application.AuditLogs.List;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Auditing;

public sealed class AuditLogService : IAuditLogService
{
    private static readonly JsonSerializerOptions MetadataSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly EmploymentManagementDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IClock _clock;

    public AuditLogService(
        EmploymentManagementDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IHttpContextAccessor httpContextAccessor,
        IClock clock)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _httpContextAccessor = httpContextAccessor;
        _clock = clock;
    }

    public async Task WriteAsync(AuditWriteEntry entry, CancellationToken cancellationToken)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = entry.ActorUserId ?? _currentUserContext.UserId,
            SessionId = entry.SessionId ?? _currentUserContext.SessionId,
            ActionType = entry.ActionType,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            Result = entry.Result,
            CorrelationId = entry.CorrelationId ?? _httpContextAccessor.HttpContext?.TraceIdentifier ?? string.Empty,
            OccurredAtUtc = _clock.UtcNow,
            MetadataJson = entry.Metadata is null ? null : JsonSerializer.Serialize(entry.Metadata, MetadataSerializerOptions)
        };

        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result> ListAsync(Query query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize switch
        {
            <= 0 => 50,
            > 100 => 100,
            _ => query.PageSize
        };

        var dbQuery = _dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (query.ActorUserId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.ActorUserId == query.ActorUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.ActionType))
        {
            dbQuery = dbQuery.Where(x => x.ActionType == query.ActionType);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            dbQuery = dbQuery.Where(x => x.EntityType == query.EntityType);
        }

        if (query.EntityId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.EntityId == query.EntityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Result))
        {
            dbQuery = dbQuery.Where(x => x.Result == query.Result);
        }

        if (query.FromUtc.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.OccurredAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.OccurredAtUtc <= query.ToUtc.Value);
        }

        dbQuery = dbQuery.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id);

        var totalCount = await dbQuery.CountAsync(cancellationToken);
        var items = await dbQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new Item(
                x.Id,
                x.ActorUserId,
                x.SessionId,
                x.ActionType,
                x.EntityType,
                x.EntityId,
                x.Result,
                x.CorrelationId,
                x.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        return new Result(items, page, pageSize, totalCount);
    }
}
