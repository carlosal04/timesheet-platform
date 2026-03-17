using Microsoft.EntityFrameworkCore;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Authentication;

public sealed class SessionValidator : ISessionValidator
{
    private readonly EmploymentManagementDbContext _dbContext;
    private readonly IClock _clock;

    public SessionValidator(EmploymentManagementDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<SessionValidationResult> ValidateAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;

        var user = await _dbContext.Users
            .Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null || user.Role is null || !user.IsActive || !user.Role.IsActive || user.IsLockedOut(nowUtc))
        {
            return SessionValidationResult.Failure();
        }

        var session = await _dbContext.UserSessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken);
        if (session is null || !session.IsActive(nowUtc) || session.SessionVersion != user.SessionVersion)
        {
            return SessionValidationResult.Failure();
        }

        session.LastSeenAtUtc = nowUtc;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return SessionValidationResult.Success(user, user.Role.Code);
    }
}
