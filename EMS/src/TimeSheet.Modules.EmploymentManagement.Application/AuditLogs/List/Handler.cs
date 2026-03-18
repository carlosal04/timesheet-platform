using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Audit;

namespace TimeSheet.Modules.EmploymentManagement.Application.AuditLogs.List;

public sealed class Handler
{
    private readonly IAuditLogService _auditLogService;
    private readonly IValidator<Query> _validator;

    public Handler(IAuditLogService auditLogService, IValidator<Query> validator)
    {
        _auditLogService = auditLogService;
        _validator = validator;
    }

    public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(query, cancellationToken);
        return await _auditLogService.ListAsync(query, cancellationToken);
    }
}
