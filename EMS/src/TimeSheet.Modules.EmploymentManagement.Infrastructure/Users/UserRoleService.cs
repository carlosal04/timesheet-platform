using Microsoft.EntityFrameworkCore;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Audit;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Users;
using TimeSheet.Modules.EmploymentManagement.Application.Common.Exceptions;
using Command = TimeSheet.Modules.EmploymentManagement.Application.Users.AssignRole.Command;
using Result = TimeSheet.Modules.EmploymentManagement.Application.Users.AssignRole.Result;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Users;

public sealed class UserRoleService : IUserRoleService
{
    private readonly EmploymentManagementDbContext _dbContext;
    private readonly IUserSessionAuthenticationService _userSessionAuthenticationService;
    private readonly IAuditLogService _auditLogService;

    public UserRoleService(
        EmploymentManagementDbContext dbContext,
        IUserSessionAuthenticationService userSessionAuthenticationService,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _userSessionAuthenticationService = userSessionAuthenticationService;
        _auditLogService = auditLogService;
    }

    public async Task<Result> AssignRoleAsync(Command command, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == command.UserId && x.IsActive, cancellationToken);

        if (user is null)
        {
            await WriteRejectedAsync(command.UserId, command.RoleId, AuditResults.NotFound, "UserNotFound", cancellationToken);
            throw ProblemExceptions.NotFound("User was not found.");
        }

        var targetRole = await _dbContext.Roles
            .SingleOrDefaultAsync(x => x.Id == command.RoleId, cancellationToken);

        if (targetRole is null)
        {
            await WriteRejectedAsync(user.Id, command.RoleId, AuditResults.NotFound, "RoleNotFound", cancellationToken);
            throw ProblemExceptions.NotFound("Role was not found.");
        }

        if (!targetRole.IsActive)
        {
            await WriteRejectedAsync(user.Id, targetRole.Id, AuditResults.Conflict, "InactiveRole", cancellationToken);
            throw ProblemExceptions.Conflict("Target role is inactive.");
        }

        if (user.Role?.Code == RoleCodes.Admin && targetRole.Code != RoleCodes.Admin)
        {
            var activeAdminCount = await _dbContext.Users
                .Include(x => x.Role)
                .CountAsync(
                    x => x.IsActive
                        && x.Role != null
                        && x.Role.IsActive
                        && x.Role.Code == RoleCodes.Admin,
                    cancellationToken);

            if (activeAdminCount <= 1)
            {
                await WriteRejectedAsync(user.Id, targetRole.Id, AuditResults.Conflict, "ZeroActiveAdmins", cancellationToken);
                throw ProblemExceptions.Conflict("Role change would leave zero active Admin users.");
            }
        }

        if (user.RoleId == targetRole.Id)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.UserRoleAssigned,
                    AuditEntityTypes.User,
                    AuditResults.Success,
                    user.Id,
                    new { targetRole.Id, targetRole.Code, SessionsRevoked = 0, Changed = false }),
                cancellationToken);

            return new Result(user.Id, targetRole.Id, targetRole.Code, 0);
        }

        user.RoleId = targetRole.Id;
        user.Role = targetRole;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var sessionsRevoked = await _userSessionAuthenticationService.RevokeActiveSessionsAsync(user.Id, "RoleChange", cancellationToken);

        await _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.UserRoleAssigned,
                AuditEntityTypes.User,
                AuditResults.Success,
                user.Id,
                new { targetRole.Id, targetRole.Code, SessionsRevoked = sessionsRevoked, Changed = true }),
            cancellationToken);

        return new Result(user.Id, targetRole.Id, targetRole.Code, sessionsRevoked);
    }

    private Task WriteRejectedAsync(
        Guid userId,
        Guid roleId,
        string result,
        string reason,
        CancellationToken cancellationToken)
    {
        return _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.RoleAssignmentRejected,
                AuditEntityTypes.User,
                result,
                userId,
                new { RoleId = roleId, Reason = reason }),
            cancellationToken);
    }
}
