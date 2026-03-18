using Microsoft.EntityFrameworkCore;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Addresses;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Audit;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
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
    private readonly IClock _clock;

    public EmployeeAddressService(
        EmploymentManagementDbContext dbContext,
        IAuditLogService auditLogService,
        IClock clock)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _clock = clock;
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
        return GetCoreAsync(query, cancellationToken);
    }

    public async Task<CreateResult> CreateAsync(CreateCommand command, CancellationToken cancellationToken)
    {
        var employee = await _dbContext.Employees
            .SingleOrDefaultAsync(x => x.Id == command.EmployeeId && x.DeletedAtUtc == null, cancellationToken);

        if (employee is null)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.AddressCreated,
                    AuditEntityTypes.EmployeeAddress,
                    AuditResults.NotFound,
                    command.EmployeeId),
                cancellationToken);

            throw ProblemExceptions.NotFound("Employee was not found.");
        }

        if (command.IsPrimary)
        {
            await ClearActivePrimaryAddressesAsync(command.EmployeeId, excludedAddressId: null, cancellationToken);
        }

        var address = new Domain.Employees.EmployeeAddress
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            AddressType = command.AddressType.Trim(),
            IsPrimary = command.IsPrimary,
            Line1 = command.Line1.Trim(),
            Line2 = NormalizeOptional(command.Line2),
            City = command.City.Trim(),
            State = command.State.Trim(),
            ZipCode = command.ZipCode.Trim(),
            CountryCode = command.CountryCode.Trim().ToUpperInvariant(),
            CreatedAtUtc = _clock.UtcNow
        };

        _dbContext.EmployeeAddresses.Add(address);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.AddressCreated,
                AuditEntityTypes.EmployeeAddress,
                AuditResults.Success,
                address.Id,
                new { employee.Id, address.IsPrimary }),
            cancellationToken);

        return new CreateResult(address.Id, employee.Id);
    }

    public async Task<UpdateResult> UpdateAsync(UpdateCommand command, CancellationToken cancellationToken)
    {
        var employee = await _dbContext.Employees
            .SingleOrDefaultAsync(x => x.Id == command.EmployeeId && x.DeletedAtUtc == null, cancellationToken);

        if (employee is null)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.AddressUpdated,
                    AuditEntityTypes.EmployeeAddress,
                    AuditResults.NotFound,
                    command.AddressId),
                cancellationToken);

            throw ProblemExceptions.NotFound("Employee was not found.");
        }

        var address = await _dbContext.EmployeeAddresses
            .SingleOrDefaultAsync(
                x => x.EmployeeId == command.EmployeeId
                    && x.Id == command.AddressId
                    && x.DeletedAtUtc == null,
                cancellationToken);

        if (address is null)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.AddressUpdated,
                    AuditEntityTypes.EmployeeAddress,
                    AuditResults.NotFound,
                    command.AddressId),
                cancellationToken);

            throw ProblemExceptions.NotFound("Address was not found.");
        }

        if (command.IsPrimary)
        {
            await ClearActivePrimaryAddressesAsync(command.EmployeeId, command.AddressId, cancellationToken);
        }

        address.AddressType = command.AddressType.Trim();
        address.IsPrimary = command.IsPrimary;
        address.Line1 = command.Line1.Trim();
        address.Line2 = NormalizeOptional(command.Line2);
        address.City = command.City.Trim();
        address.State = command.State.Trim();
        address.ZipCode = command.ZipCode.Trim();
        address.CountryCode = command.CountryCode.Trim().ToUpperInvariant();

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.AddressUpdated,
                AuditEntityTypes.EmployeeAddress,
                AuditResults.Success,
                address.Id,
                new { employee.Id, address.IsPrimary }),
            cancellationToken);

        return new UpdateResult(address.Id, employee.Id);
    }

    public async Task SetPrimaryAsync(SetPrimaryCommand command, CancellationToken cancellationToken)
    {
        var employeeExists = await _dbContext.Employees
            .AnyAsync(x => x.Id == command.EmployeeId && x.DeletedAtUtc == null, cancellationToken);

        if (!employeeExists)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.AddressUpdated,
                    AuditEntityTypes.EmployeeAddress,
                    AuditResults.NotFound,
                    command.AddressId),
                cancellationToken);

            throw ProblemExceptions.NotFound("Employee was not found.");
        }

        var targetAddress = await _dbContext.EmployeeAddresses
            .SingleOrDefaultAsync(
                x => x.EmployeeId == command.EmployeeId && x.Id == command.AddressId,
                cancellationToken);

        if (targetAddress is null)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.AddressUpdated,
                    AuditEntityTypes.EmployeeAddress,
                    AuditResults.NotFound,
                    command.AddressId),
                cancellationToken);

            throw ProblemExceptions.NotFound("Address was not found.");
        }

        if (targetAddress.DeletedAtUtc is not null)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.AddressUpdated,
                    AuditEntityTypes.EmployeeAddress,
                    AuditResults.Conflict,
                    command.AddressId),
                cancellationToken);

            throw ProblemExceptions.Conflict("Address is soft-deleted.");
        }

        var previousPrimaryId = await _dbContext.EmployeeAddresses
            .Where(x => x.EmployeeId == command.EmployeeId && x.DeletedAtUtc == null && x.IsPrimary)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (!targetAddress.IsPrimary)
        {
            if (_dbContext.Database.IsRelational())
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                await ClearActivePrimaryAddressesAsync(command.EmployeeId, command.AddressId, cancellationToken);
                targetAddress.IsPrimary = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await ClearActivePrimaryAddressesAsync(command.EmployeeId, command.AddressId, cancellationToken);
                targetAddress.IsPrimary = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        await _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.AddressUpdated,
                AuditEntityTypes.EmployeeAddress,
                AuditResults.Success,
                targetAddress.Id,
                new
                {
                    PreviousPrimaryAddressId = previousPrimaryId,
                    NewPrimaryAddressId = targetAddress.Id
                }),
            cancellationToken);
    }

    public Task DeleteAsync(DeleteCommand command, CancellationToken cancellationToken)
    {
        return DeleteCoreAsync(command, cancellationToken);
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

    private async Task<GetByIdAddress?> GetCoreAsync(GetByIdQuery query, CancellationToken cancellationToken)
    {
        var employeeExists = await _dbContext.Employees
            .AnyAsync(x => x.Id == query.EmployeeId && x.DeletedAtUtc == null, cancellationToken);

        if (!employeeExists)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.AddressRead,
                    AuditEntityTypes.EmployeeAddress,
                    AuditResults.NotFound,
                    query.AddressId),
                cancellationToken);

            throw ProblemExceptions.NotFound("Employee was not found.");
        }

        var address = await _dbContext.EmployeeAddresses
            .AsNoTracking()
            .Where(x => x.EmployeeId == query.EmployeeId && x.Id == query.AddressId && x.DeletedAtUtc == null)
            .Select(x => new GetByIdAddress(
                x.Id,
                x.EmployeeId,
                x.AddressType,
                x.IsPrimary,
                x.Line1,
                x.Line2,
                x.City,
                x.State,
                x.ZipCode,
                x.CountryCode))
            .SingleOrDefaultAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.AddressRead,
                AuditEntityTypes.EmployeeAddress,
                address is null ? AuditResults.NotFound : AuditResults.Success,
                query.AddressId),
            cancellationToken);

        return address;
    }

    private async Task DeleteCoreAsync(DeleteCommand command, CancellationToken cancellationToken)
    {
        var employeeExists = await _dbContext.Employees
            .AnyAsync(x => x.Id == command.EmployeeId && x.DeletedAtUtc == null, cancellationToken);

        if (!employeeExists)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.AddressSoftDeleted,
                    AuditEntityTypes.EmployeeAddress,
                    AuditResults.NotFound,
                    command.AddressId),
                cancellationToken);

            throw ProblemExceptions.NotFound("Employee was not found.");
        }

        var address = await _dbContext.EmployeeAddresses
            .SingleOrDefaultAsync(
                x => x.EmployeeId == command.EmployeeId && x.Id == command.AddressId,
                cancellationToken);

        if (address is null)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.AddressSoftDeleted,
                    AuditEntityTypes.EmployeeAddress,
                    AuditResults.NotFound,
                    command.AddressId),
                cancellationToken);

            throw ProblemExceptions.NotFound("Address was not found.");
        }

        if (address.DeletedAtUtc is not null)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.AddressSoftDeleted,
                    AuditEntityTypes.EmployeeAddress,
                    AuditResults.Conflict,
                    command.AddressId),
                cancellationToken);

            throw ProblemExceptions.Conflict("Address is already deleted.");
        }

        var wasPrimary = address.IsPrimary;
        var replacementAddress = wasPrimary
            ? await _dbContext.EmployeeAddresses
                .Where(x => x.EmployeeId == command.EmployeeId && x.Id != command.AddressId && x.DeletedAtUtc == null)
                .OrderBy(x => x.CreatedAtUtc)
                .ThenBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        if (_dbContext.Database.IsRelational())
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await SoftDeleteAddressAsync(address, replacementAddress, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        else
        {
            await SoftDeleteAddressAsync(address, replacementAddress, cancellationToken);
        }

        await _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.AddressSoftDeleted,
                AuditEntityTypes.EmployeeAddress,
                AuditResults.Success,
                address.Id,
                new
                {
                    WasPrimary = wasPrimary,
                    ReplacementPrimaryAddressId = replacementAddress?.Id
                }),
            cancellationToken);

        if (replacementAddress is not null)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.AddressPrimaryChanged,
                    AuditEntityTypes.EmployeeAddress,
                    AuditResults.Success,
                    replacementAddress.Id,
                    new
                    {
                        PreviousPrimaryAddressId = address.Id,
                        NewPrimaryAddressId = replacementAddress.Id,
                        Reason = "ReplacementAfterDelete"
                    }),
                cancellationToken);
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task ClearActivePrimaryAddressesAsync(
        Guid employeeId,
        Guid? excludedAddressId,
        CancellationToken cancellationToken)
    {
        var activePrimaryAddresses = await _dbContext.EmployeeAddresses
            .Where(x => x.EmployeeId == employeeId && x.DeletedAtUtc == null && x.IsPrimary)
            .Where(x => !excludedAddressId.HasValue || x.Id != excludedAddressId.Value)
            .ToListAsync(cancellationToken);

        foreach (var activePrimaryAddress in activePrimaryAddresses)
        {
            activePrimaryAddress.IsPrimary = false;
        }

        if (activePrimaryAddresses.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SoftDeleteAddressAsync(
        Domain.Employees.EmployeeAddress address,
        Domain.Employees.EmployeeAddress? replacementAddress,
        CancellationToken cancellationToken)
    {
        var wasPrimary = address.IsPrimary;

        address.IsPrimary = false;
        address.DeletedAtUtc = _clock.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (wasPrimary && replacementAddress is not null)
        {
            replacementAddress.IsPrimary = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
