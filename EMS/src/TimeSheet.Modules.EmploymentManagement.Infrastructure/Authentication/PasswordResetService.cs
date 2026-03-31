using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Audit;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Email;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Configuration;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Authentication;

public sealed class PasswordResetService : IPasswordResetService
{
    private readonly EmploymentManagementDbContext _dbContext;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IUserSessionAuthenticationService _userSessionAuthenticationService;
    private readonly IUserAccessEmailComposer _userAccessEmailComposer;
    private readonly IEmailSender _emailSender;
    private readonly IAuditLogService _auditLogService;
    private readonly IClock _clock;
    private readonly FrontendOptions _frontendOptions;

    public PasswordResetService(
        EmploymentManagementDbContext dbContext,
        IPasswordHashingService passwordHashingService,
        IUserSessionAuthenticationService userSessionAuthenticationService,
        IUserAccessEmailComposer userAccessEmailComposer,
        IEmailSender emailSender,
        IAuditLogService auditLogService,
        IClock clock,
        IOptions<FrontendOptions> frontendOptions)
    {
        _dbContext = dbContext;
        _passwordHashingService = passwordHashingService;
        _userSessionAuthenticationService = userSessionAuthenticationService;
        _userAccessEmailComposer = userAccessEmailComposer;
        _emailSender = emailSender;
        _auditLogService = auditLogService;
        _clock = clock;
        _frontendOptions = frontendOptions.Value;
    }

    public async Task RequestAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null || user.Role is null || !user.IsActive || !user.Role.IsActive || user.MustChangePassword)
        {
            return;
        }

        var nowUtc = _clock.UtcNow;
        var expiresAtUtc = nowUtc.AddHours(1);
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var tokenHash = HashToken(token);

        user.SetPasswordResetState(tokenHash, expiresAtUtc);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var resetUrl = new Uri(new Uri(_frontendOptions.BaseUrl.TrimEnd('/') + "/"), $"reset-password?token={Uri.EscapeDataString(token)}").ToString();
        var emailMessage = _userAccessEmailComposer.ComposePasswordReset(
            new PasswordResetEmailModel(
                new EmailRecipient(user.Email),
                resetUrl,
                expiresAtUtc));

        await _emailSender.SendAsync(emailMessage, cancellationToken);

        await _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.PasswordResetRequested,
                AuditEntityTypes.User,
                AuditResults.Success,
                user.Id,
                new { ExpiresAtUtc = expiresAtUtc, user.Email }),
            cancellationToken);
    }

    public async Task<bool> ResetAsync(string token, string newPassword, CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        var tokenHash = HashToken(token);

        var user = await _dbContext.Users
            .Include(x => x.Role)
            .SingleOrDefaultAsync(
                x => x.PasswordResetTokenHash == tokenHash,
                cancellationToken);

        if (user is null
            || user.Role is null
            || !user.IsActive
            || !user.Role.IsActive
            || !user.PasswordResetTokenExpiresAtUtc.HasValue
            || user.PasswordResetTokenExpiresAtUtc.Value <= nowUtc)
        {
            return false;
        }

        user.PasswordHash = _passwordHashingService.HashPassword(user, newPassword);
        user.ClearPasswordResetState();

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _userSessionAuthenticationService.RevokeActiveSessionsAsync(user.Id, "PasswordReset", cancellationToken);

        await _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.PasswordChanged,
                AuditEntityTypes.User,
                AuditResults.Success,
                user.Id,
                new { Reason = "PasswordReset" },
                user.Id),
            cancellationToken);

        return true;
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
