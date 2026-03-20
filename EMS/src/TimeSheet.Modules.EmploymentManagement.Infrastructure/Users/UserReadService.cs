using Microsoft.EntityFrameworkCore;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Users;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;
using Item = TimeSheet.Modules.EmploymentManagement.Application.Users.List.Item;
using Request = TimeSheet.Modules.EmploymentManagement.Application.Users.List.Request;
using Result = TimeSheet.Modules.EmploymentManagement.Application.Users.List.Result;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Users;

public sealed class UserReadService : IUserReadService
{
    private readonly EmploymentManagementDbContext _dbContext;

    public UserReadService(EmploymentManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> ListAsync(Request request, CancellationToken cancellationToken)
    {
        var emailFilter = request.Email?.Trim().ToLowerInvariant();
        var roleCodeFilter = request.RoleCode?.Trim().ToLowerInvariant();

        var dbQuery =
            from user in _dbContext.Users.AsNoTracking()
            join role in _dbContext.Roles.AsNoTracking() on user.RoleId equals role.Id
            join employee in _dbContext.Employees.AsNoTracking() on user.EmployeeId equals employee.Id into employeeJoin
            from employee in employeeJoin.DefaultIfEmpty()
            select new
            {
                User = user,
                Role = role,
                EmployeeFirstName = employee != null ? employee.FirstName : null,
                EmployeeLastName = employee != null ? employee.LastName : null
            };

        if (!request.IncludeInactive)
        {
            dbQuery = dbQuery.Where(x => x.User.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(emailFilter))
        {
            dbQuery = dbQuery.Where(x => x.User.Email.ToLower().Contains(emailFilter));
        }

        if (!string.IsNullOrWhiteSpace(roleCodeFilter))
        {
            dbQuery = dbQuery.Where(x => x.Role.Code.ToLower() == roleCodeFilter);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var rows = await dbQuery
            .OrderBy(x => x.User.Email)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new
            {
                UserId = x.User.Id,
                UserEmail = x.User.Email,
                RoleId = x.Role.Id,
                RoleCode = x.Role.Code,
                RoleName = x.Role.Name,
                x.User.EmployeeId,
                x.EmployeeFirstName,
                x.EmployeeLastName,
                x.User.IsActive
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(x => new Item(
                x.UserId,
                x.UserEmail,
                x.RoleId,
                x.RoleCode,
                x.RoleName,
                x.EmployeeId,
                BuildEmployeeName(x.EmployeeFirstName, x.EmployeeLastName),
                x.IsActive))
            .ToList();

        return new Result(items, request.Page, request.PageSize, totalCount);
    }

    private static string? BuildEmployeeName(string? firstName, string? lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
        {
            return null;
        }

        return $"{firstName} {lastName}".Trim();
    }
}
