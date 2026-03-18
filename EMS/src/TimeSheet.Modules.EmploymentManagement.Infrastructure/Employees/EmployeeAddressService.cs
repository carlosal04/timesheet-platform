using Microsoft.EntityFrameworkCore;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Addresses;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Audit;
using TimeSheet.Modules.EmploymentManagement.Application.Common.Exceptions;
using CreateCommand = TimeSheet.Modules.EmploymentManagement.Application.Addresses.Create.Command;
using CreateResult = TimeSheet.Modules.EmploymentManagement.Application.Addresses.Create.Result;
using DeleteCommand = TimeSheet.Modules.EmploymentManagement.Application.Addresses.Delete.Command;
using DeleteOwnCommand = TimeSheet.Modules.EmploymentManagement.Application.Addresses.DeleteOwn.Command;
using GetByIdAddress = TimeSheet.Modules.EmploymentManagement.Application.Addresses.GetById.Address;
using GetByIdQuery = TimeSheet.Modules.EmploymentManagement.Application.Addresses.GetById.Query;
using ListItem = TimeSheet.Modules.EmploymentManagement.Application.Addresses.List.Item;
using ListMineQuery = TimeSheet.Modules.EmploymentManagement.Application.Addresses.ListMine.Query;
using ListQuery = TimeSheet.Modules.EmploymentManagement.Application.Addresses.List.Query;
using ListResult = TimeSheet.Modules.EmploymentManagement.Application.Addresses.List.Result;
using SetOwnPrimaryCommand = TimeSheet.Modules.EmploymentManagement.Application.Addresses.SetOwnPrimary.Command;
using SetPrimaryCommand = TimeSheet.Modules.EmploymentManagement.Application.Addresses.SetPrimary.Command;
using UpdateCommand = TimeSheet.Modules.EmploymentManagement.Application.Addresses.Update.Command;
using UpdateResult = TimeSheet.Modules.EmploymentManagement.Application.Addresses.Update.Result;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Employees;

public sealed class EmployeeAddressService : IEmployeeAddressService
{
    private readonly EmploymentManagementDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public EmployeeAddressService(
        EmploymentManagementDbContext dbContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    public async Task<ListResult> ListAsync(ListQuery query, CancellationToken cancellationToken)
    {
        var employeeExists = await _dbContext.Employees
            .AnyAsync(x => x.Id == query.EmployeeId && x.DeletedAtUtc == null, cancellationToken);

        if (!employeeExists)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.AddressListRead,
                    AuditEntityTypes.EmployeeAddress,
                    AuditResults.NotFound,
                    query.EmployeeId),
                cancellationToken);

            throw ProblemExceptions.NotFound("Employee was not found.");
        }

        var items = await _dbContext.EmployeeAddresses
            .AsNoTracking()
            .Where(x => x.EmployeeId == query.EmployeeId && (query.IncludeDeleted || x.DeletedAtUtc == null))
            .OrderBy(x => x.DeletedAtUtc != null)
            .ThenByDescending(x => x.IsPrimary)
            .ThenBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => new ListItem(
                x.Id,
                x.AddressType,
                x.IsPrimary,
                x.Line1,
                x.Line2,
                x.City,
                x.State,
                x.ZipCode,
                x.CountryCode))
            .ToListAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.AddressListRead,
                AuditEntityTypes.EmployeeAddress,
                AuditResults.Success,
                query.EmployeeId,
                new { Count = items.Count, query.IncludeDeleted }),
            cancellationToken);

        return new ListResult(query.EmployeeId, items);
    }

    public Task<GetByIdAddress?> GetAsync(GetByIdQuery query, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Not implemented yet.");
    }

    public Task<CreateResult> CreateAsync(CreateCommand command, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Not implemented yet.");
    }

    public Task<UpdateResult> UpdateAsync(UpdateCommand command, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Not implemented yet.");
    }

    public Task SetPrimaryAsync(SetPrimaryCommand command, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Not implemented yet.");
    }

    public Task DeleteAsync(DeleteCommand command, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Not implemented yet.");
    }

    public Task<ListResult> ListOwnAsync(ListMineQuery query, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Not implemented yet.");
    }

    public Task SetOwnPrimaryAsync(SetOwnPrimaryCommand command, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Not implemented yet.");
    }

    public Task DeleteOwnAsync(DeleteOwnCommand command, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Not implemented yet.");
    }
}
