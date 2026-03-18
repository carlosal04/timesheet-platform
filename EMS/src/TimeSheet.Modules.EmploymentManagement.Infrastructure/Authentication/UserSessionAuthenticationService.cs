using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication.Configuration;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication.Login;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Authentication;

public sealed class UserSessionAuthenticationService : IUserSessionAuthenticationService
{
    private readonly EmploymentManagementDbContext _dbContext;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IClock _clock;
    private readonly AuthOptions _authOptions;

    public UserSessionAuthenticationService(
        EmploymentManagementDbContext dbContext,
        IPasswordHashingService passwordHashingService,
        IClock clock,
        IOptions<AuthOptions> authOptions)
    {
        _dbContext = dbContext;
        _passwordHashingService = passwordHashingService;
        _clock = clock;
        _authOptions = authOptions.Value;
    }

    public async Task<Result> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var nowUtc = _clock.UtcNow;

        var user = await _dbContext.Users
            .Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null || user.Role is null || !user.Role.IsActive || !user.IsActive || user.IsLockedOut(nowUtc))
        {
            if (user is not null)
            {
                user.RecordFailedAccess(_authOptions.LockoutThreshold, TimeSpan.FromMinutes(_authOptions.LockoutMinutes), nowUtc);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return Result.Failure("invalid_credentials");
        }

        if (!_passwordHashingService.VerifyPassword(user, password))
        {
            user.RecordFailedAccess(_authOptions.LockoutThreshold, TimeSpan.FromMinutes(_authOptions.LockoutMinutes), nowUtc);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Failure("invalid_credentials");
        }

        user.ResetFailedAccess();
        var sessionVersion = user.IncrementSessionVersion();

        var activeSessions = await _dbContext.UserSessions
            .Where(x => x.UserId == user.Id && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var activeSession in activeSessions)
        {
            activeSession.Revoke("ReLogin", nowUtc);
        }

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SessionVersion = sessionVersion,
            CreatedAtUtc = nowUtc,
            LastSeenAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc.AddMinutes(_authOptions.SessionLifetimeMinutes)
        };

        _dbContext.UserSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(user, session, user.Role.Code);
    }

    public async Task LogoutAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _dbContext.UserSessions.SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return;
        }

        session.Revoke("UserLogout", _clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
