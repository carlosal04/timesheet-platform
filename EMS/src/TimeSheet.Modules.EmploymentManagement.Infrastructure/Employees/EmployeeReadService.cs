using Microsoft.EntityFrameworkCore;
using TimeSheet.Modules.EmploymentManagement.Application.Employees;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Employees;

public sealed class EmployeeReadService : IEmployeeReadService
{
    private readonly EmploymentManagementDbContext _dbContext;

    public EmployeeReadService(EmploymentManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<EmployeeSummaryView>> ListAsync(EmployeeListRequest request, CancellationToken cancellationToken)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize switch
        {
            <= 0 => 25,
            > 100 => 100,
            _ => request.PageSize
        };

        var query = _dbContext.Employees.AsNoTracking().AsQueryable();

        if (!request.IncludeDeleted)
        {
            query = query.Where(x => x.DeletedAtUtc == null);
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim().ToLowerInvariant();
            query = query.Where(x =>
                (x.FirstName + " " + x.LastName).ToLower().Contains(name) ||
                x.FirstName.ToLower().Contains(name) ||
                x.LastName.ToLower().Contains(name));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            query = query.Where(x => x.Status == status);
        }

        if (request.HireDateFrom.HasValue)
        {
            query = query.Where(x => x.HireDate >= request.HireDateFrom.Value);
        }

        if (request.HireDateTo.HasValue)
        {
            query = query.Where(x => x.HireDate <= request.HireDateTo.Value);
        }

        query = query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var employees = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new EmployeeSummaryView(
                x.Id,
                x.FirstName,
                x.LastName,
                x.Email,
                x.Phone,
                x.DateOfBirth,
                x.HireDate,
                x.Status,
                request.IncludePrimaryAddress
                    ? x.Addresses
                        .Where(address => address.DeletedAtUtc == null)
                        .OrderByDescending(address => address.IsPrimary)
                        .ThenBy(address => address.CreatedAtUtc)
                        .Select(address => new EmployeePrimaryAddressView(
                            address.Id,
                            address.AddressType,
                            address.IsPrimary,
                            address.Line1,
                            address.Line2,
                            address.City,
                            address.State,
                            address.ZipCode,
                            address.CountryCode))
                        .FirstOrDefault()
                    : null))
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeSummaryView>(employees, page, pageSize, totalCount);
    }

    public async Task<EmployeeDetailView?> GetAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        return await _dbContext.Employees
            .AsNoTracking()
            .Where(x => x.Id == employeeId && x.DeletedAtUtc == null)
            .Select(x => new EmployeeDetailView(
                x.Id,
                x.FirstName,
                x.LastName,
                x.Email,
                x.Phone,
                x.DateOfBirth,
                x.HireDate,
                x.Status,
                x.Addresses
                    .Where(address => address.DeletedAtUtc == null)
                    .OrderByDescending(address => address.IsPrimary)
                    .ThenBy(address => address.CreatedAtUtc)
                    .Select(address => new EmployeeAddressView(
                        address.Id,
                        address.AddressType,
                        address.IsPrimary,
                        address.Line1,
                        address.Line2,
                        address.City,
                        address.State,
                        address.ZipCode,
                        address.CountryCode))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
