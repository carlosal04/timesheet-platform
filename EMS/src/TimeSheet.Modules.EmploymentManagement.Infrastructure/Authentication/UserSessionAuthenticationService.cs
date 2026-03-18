using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Audit;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication.Configuration;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication.Login;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Authentication;

public sealed class UserSessionAuthenticationService : IUserSessionAuthenticationService
{
    private readonly EmploymentManagementDbContext _dbContext;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IClock _clock;
    private readonly AuthOptions _authOptions;
    private readonly IAuditLogService _auditLogService;

    public UserSessionAuthenticationService(
        EmploymentManagementDbContext dbContext,
        IPasswordHashingService passwordHashingService,
        IClock clock,
        IOptions<AuthOptions> authOptions,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _passwordHashingService = passwordHashingService;
        _clock = clock;
        _authOptions = authOptions.Value;
        _auditLogService = auditLogService;
    }

    public async Task<Result> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var nowUtc = _clock.UtcNow;

        var user = await _dbContext.Users
            .Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null || user.Role is null || !user.Role.IsActive || !user.IsActive)
        {
            await WriteFailedLoginAuditAsync(user?.Id, normalizedEmail, AuditResults.Failure, cancellationToken);
            return Result.Failure("invalid_credentials");
        }

        if (user.IsLockedOut(nowUtc))
        {
            await WriteFailedLoginAuditAsync(user.Id, normalizedEmail, AuditResults.Rejected, cancellationToken);
            return Result.Failure("locked_out");
        }

        if (!_passwordHashingService.VerifyPassword(user, password))
        {
            user.RecordFailedAccess(_authOptions.LockoutThreshold, TimeSpan.FromMinutes(_authOptions.LockoutMinutes), nowUtc);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (user.IsLockedOut(nowUtc))
            {
                await _auditLogService.WriteAsync(
                    new AuditWriteEntry(
                        AuditActionTypes.AccountLockedOut,
                        AuditEntityTypes.User,
                        AuditResults.Rejected,
                        user.Id,
                        new { user.Email }),
                    cancellationToken);

                await WriteFailedLoginAuditAsync(user.Id, normalizedEmail, AuditResults.Rejected, cancellationToken);
                return Result.Failure("locked_out");
            }

            await WriteFailedLoginAuditAsync(user.Id, normalizedEmail, AuditResults.Failure, cancellationToken);
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

        foreach (var revokedSession in activeSessions)
        {
            await _auditLogService.WriteAsync(
                new AuditWriteEntry(
                    AuditActionTypes.SessionRevoked,
                    AuditEntityTypes.UserSession,
                    AuditResults.Success,
                    revokedSession.Id,
                    new { Reason = "ReLogin" },
                    user.Id,
                    revokedSession.Id),
                cancellationToken);
        }

        await _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.SessionCreated,
                AuditEntityTypes.UserSession,
                AuditResults.Success,
                session.Id,
                new { user.Email },
                user.Id,
                session.Id),
            cancellationToken);

        await _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.LoginSucceeded,
                AuditEntityTypes.Authentication,
                AuditResults.Success,
                user.Id,
                new { user.Email },
                user.Id,
                session.Id),
            cancellationToken);

        return Result.Success(user, session, user.Role.Code);
    }

    public async Task LogoutAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _dbContext.UserSessions.SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return;
        }

        var nowUtc = _clock.UtcNow;
        session.Revoke("UserLogout", nowUtc);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.SessionRevoked,
                AuditEntityTypes.UserSession,
                AuditResults.Success,
                session.Id,
                new { Reason = "UserLogout" },
                session.UserId,
                session.Id),
            cancellationToken);

        await _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.LogoutSucceeded,
                AuditEntityTypes.Authentication,
                AuditResults.Success,
                session.UserId,
                null,
                session.UserId,
                session.Id),
            cancellationToken);
    }

    private Task WriteFailedLoginAuditAsync(Guid? userId, string email, string result, CancellationToken cancellationToken)
    {
        return _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.LoginFailed,
                AuditEntityTypes.Authentication,
                result,
                userId,
                new { Email = email },
                userId),
            cancellationToken);
    }
}
