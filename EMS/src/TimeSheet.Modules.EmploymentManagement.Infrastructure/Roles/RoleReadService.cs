using Microsoft.EntityFrameworkCore;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Roles;
using Item = TimeSheet.Modules.EmploymentManagement.Application.Roles.List.Item;
using Query = TimeSheet.Modules.EmploymentManagement.Application.Roles.List.Query;
using Result = TimeSheet.Modules.EmploymentManagement.Application.Roles.List.Result;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Roles;

public sealed class RoleReadService : IRoleReadService
{
    private readonly EmploymentManagementDbContext _dbContext;

    public RoleReadService(EmploymentManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> ListAsync(Query query, CancellationToken cancellationToken)
    {
        var dbQuery = _dbContext.Roles.AsNoTracking().AsQueryable();

        if (!query.IncludeInactive)
        {
            dbQuery = dbQuery.Where(x => x.IsActive);
        }

        var items = await dbQuery
            .OrderBy(x => x.Code)
            .ThenBy(x => x.Name)
            .Select(x => new Item(
                x.Id,
                x.Code,
                x.Name,
                x.IsActive,
                x.IsSystem))
            .ToListAsync(cancellationToken);

        return new Result(items);
    }
}
