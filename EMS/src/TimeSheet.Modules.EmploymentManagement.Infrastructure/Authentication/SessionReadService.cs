using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication.Configuration;
using Result = TimeSheet.Modules.EmploymentManagement.Application.Authentication.GetSession.Result;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Authentication;

public sealed class SessionReadService : ISessionReadService
{
    private readonly EmploymentManagementDbContext _dbContext;
    private readonly IClock _clock;
    private readonly AuthOptions _authOptions;

    public SessionReadService(
        EmploymentManagementDbContext dbContext,
        IClock clock,
        IOptions<AuthOptions> authOptions)
    {
        _dbContext = dbContext;
        _clock = clock;
        _authOptions = authOptions.Value;
    }

    public async Task<Result?> GetAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;

        var session = await _dbContext.UserSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken);

        if (session is null || !session.IsActive(nowUtc))
        {
            return null;
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null || user.Role is null || !user.IsActive || !user.Role.IsActive || user.IsLockedOut(nowUtc))
        {
            return null;
        }

        return new Result(
            user.Id,
            user.Email,
            user.Role.Code,
            user.EmployeeId,
            user.MustChangePassword,
            session.Id,
            session.ExpiresAtUtc,
            _authOptions.SessionLifetimeMinutes);
    }
}
