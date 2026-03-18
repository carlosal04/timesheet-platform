using Microsoft.EntityFrameworkCore;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Audit;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Employees;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Application.Common.Exceptions;
using CreateAddress = TimeSheet.Modules.EmploymentManagement.Application.Employees.Create.Address;
using CreateCommand = TimeSheet.Modules.EmploymentManagement.Application.Employees.Create.Command;
using CreateResult = TimeSheet.Modules.EmploymentManagement.Application.Employees.Create.Result;
using DeleteCommand = TimeSheet.Modules.EmploymentManagement.Application.Employees.Delete.Command;
using UpdateCommand = TimeSheet.Modules.EmploymentManagement.Application.Employees.Update.Command;
using UpdateResult = TimeSheet.Modules.EmploymentManagement.Application.Employees.Update.Result;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;
using TimeSheet.Modules.EmploymentManagement.Domain.Employees;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Employees;

public sealed class EmployeeWriteService : IEmployeeWriteService
{
    private readonly EmploymentManagementDbContext _dbContext;
    private readonly IClock _clock;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserContext _currentUserContext;

    public EmployeeWriteService(
        EmploymentManagementDbContext dbContext,
        IClock clock,
        IAuditLogService auditLogService,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _clock = clock;
        _auditLogService = auditLogService;
        _currentUserContext = currentUserContext;
    }

    public async Task<CreateResult> CreateAsync(CreateCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(command.Email);
        var addresses = command.Addresses ?? Array.Empty<CreateAddress>();
        var nowUtc = _clock.UtcNow;

        if (addresses.Count(address => address.IsPrimary) > 1)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.EmployeeCreated,
                    AuditEntityTypes.Employee,
                    AuditResults.Conflict,
                    null,
                    new { Reason = "MultiplePrimaryAddresses" }),
                cancellationToken);

            throw ProblemExceptions.Conflict("Invalid primary-address rule.");
        }

        var duplicateExists = await _dbContext.Employees
            .AnyAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (duplicateExists)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.EmployeeCreated,
                    AuditEntityTypes.Employee,
                    AuditResults.Conflict,
                    null,
                    new { Email = normalizedEmail, Reason = "DuplicateEmployeeEmail" }),
                cancellationToken);

            throw ProblemExceptions.Conflict("An employee with this email already exists.");
        }

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            Email = normalizedEmail,
            Phone = command.Phone.Trim(),
            DateOfBirth = command.DateOfBirth,
            HireDate = command.HireDate,
            Status = command.Status.Trim(),
            CreatedAtUtc = nowUtc
        };

        foreach (var address in addresses)
        {
            employee.Addresses.Add(new EmployeeAddress
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                AddressType = address.AddressType.Trim(),
                IsPrimary = address.IsPrimary,
                Line1 = address.Line1.Trim(),
                Line2 = NormalizeOptional(address.Line2),
                City = address.City.Trim(),
                State = address.State.Trim(),
                ZipCode = address.ZipCode.Trim(),
                CountryCode = address.CountryCode.Trim().ToUpperInvariant(),
                CreatedAtUtc = nowUtc
            });
        }

        _dbContext.Employees.Add(employee);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.EmployeeCreated,
                AuditEntityTypes.Employee,
                AuditResults.Success,
                employee.Id,
                new { employee.Email, AddressCount = employee.Addresses.Count }),
            cancellationToken);

        foreach (var address in employee.Addresses)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.AddressCreated,
                    AuditEntityTypes.EmployeeAddress,
                    AuditResults.Success,
                    address.Id,
                    new { employee.Id, address.IsPrimary }),
                cancellationToken);
        }

        return new CreateResult(employee.Id);
    }

    public async Task<UpdateResult> UpdateAsync(UpdateCommand command, CancellationToken cancellationToken)
    {
        var employee = await _dbContext.Employees
            .SingleOrDefaultAsync(x => x.Id == command.EmployeeId, cancellationToken);

        if (employee is null || employee.DeletedAtUtc is not null)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.EmployeeUpdated,
                    AuditEntityTypes.Employee,
                    AuditResults.NotFound,
                    command.EmployeeId),
                cancellationToken);

            throw ProblemExceptions.NotFound("Employee was not found.");
        }

        var normalizedEmail = NormalizeEmail(command.Email);
        var duplicateExists = await _dbContext.Employees
            .AnyAsync(x => x.Id != command.EmployeeId && x.Email == normalizedEmail, cancellationToken);

        if (duplicateExists)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.EmployeeUpdated,
                    AuditEntityTypes.Employee,
                    AuditResults.Conflict,
                    employee.Id,
                    new { Email = normalizedEmail, Reason = "DuplicateEmployeeEmail" }),
                cancellationToken);

            throw ProblemExceptions.Conflict("An employee with this email already exists.");
        }

        employee.FirstName = command.FirstName.Trim();
        employee.LastName = command.LastName.Trim();
        employee.Email = normalizedEmail;
        employee.Phone = command.Phone.Trim();
        employee.DateOfBirth = command.DateOfBirth;
        employee.HireDate = command.HireDate;
        employee.Status = command.Status.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.EmployeeUpdated,
                AuditEntityTypes.Employee,
                AuditResults.Success,
                employee.Id,
                new
                {
                    employee.Email,
                    Fields = new[]
                    {
                        nameof(employee.FirstName),
                        nameof(employee.LastName),
                        nameof(employee.Email),
                        nameof(employee.Phone),
                        nameof(employee.DateOfBirth),
                        nameof(employee.HireDate),
                        nameof(employee.Status)
                    }
                }),
            cancellationToken);

        return new UpdateResult(employee.Id);
    }

    public async Task DeleteAsync(DeleteCommand command, CancellationToken cancellationToken)
    {
        var employee = await _dbContext.Employees
            .SingleOrDefaultAsync(x => x.Id == command.EmployeeId, cancellationToken);

        if (employee is null)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.EmployeeSoftDeleted,
                    AuditEntityTypes.Employee,
                    AuditResults.NotFound,
                    command.EmployeeId),
                cancellationToken);

            throw ProblemExceptions.NotFound("Employee was not found.");
        }

        if (employee.DeletedAtUtc is not null)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.EmployeeSoftDeleted,
                    AuditEntityTypes.Employee,
                    AuditResults.Conflict,
                    employee.Id),
                cancellationToken);

            throw ProblemExceptions.Conflict("Employee is already deleted.");
        }

        employee.DeletedAtUtc = _clock.UtcNow;
        employee.DeletedByUserId = _currentUserContext.UserId;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.EmployeeSoftDeleted,
                AuditEntityTypes.Employee,
                AuditResults.Success,
                employee.Id),
            cancellationToken);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
