using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Audit;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Email;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Users;
using TimeSheet.Modules.EmploymentManagement.Application.Common.Exceptions;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Configuration;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;
using Command = TimeSheet.Modules.EmploymentManagement.Application.Users.Create.Command;
using Result = TimeSheet.Modules.EmploymentManagement.Application.Users.Create.Result;
using ResendResult = TimeSheet.Modules.EmploymentManagement.Application.Users.ResendTemporaryPassword.Result;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Users;

public sealed class UserProvisioningService : IUserProvisioningService
{
    private static readonly char[] Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();
    private static readonly char[] Lowercase = "abcdefghijkmnopqrstuvwxyz".ToCharArray();
    private static readonly char[] Digits = "23456789".ToCharArray();
    private static readonly char[] Symbols = "!@$%*+-_=?.#".ToCharArray();
    private static readonly char[] AllCharacters = (new string(Uppercase) + new string(Lowercase) + new string(Digits) + new string(Symbols)).ToCharArray();

    private readonly EmploymentManagementDbContext _dbContext;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IClock _clock;
    private readonly IUserAccessEmailComposer _userAccessEmailComposer;
    private readonly IEmailSender _emailSender;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserSessionAuthenticationService _userSessionAuthenticationService;
    private readonly FrontendOptions _frontendOptions;

    public UserProvisioningService(
        EmploymentManagementDbContext dbContext,
        IPasswordHashingService passwordHashingService,
        IClock clock,
        IUserAccessEmailComposer userAccessEmailComposer,
        IEmailSender emailSender,
        IAuditLogService auditLogService,
        IUserSessionAuthenticationService userSessionAuthenticationService,
        IOptions<FrontendOptions> frontendOptions)
    {
        _dbContext = dbContext;
        _passwordHashingService = passwordHashingService;
        _clock = clock;
        _userAccessEmailComposer = userAccessEmailComposer;
        _emailSender = emailSender;
        _auditLogService = auditLogService;
        _userSessionAuthenticationService = userSessionAuthenticationService;
        _frontendOptions = frontendOptions.Value;
    }

    public async Task<Result> CreateAsync(Command command, CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles.SingleOrDefaultAsync(x => x.Id == command.RoleId, cancellationToken);
        if (role is null)
        {
            await WriteUserCreatedAuditAsync(null, command.RoleId, null, AuditResults.NotFound, "RoleNotFound", cancellationToken);
            throw ProblemExceptions.NotFound("Role was not found.");
        }

        if (!role.IsActive)
        {
            await WriteUserCreatedAuditAsync(null, role.Id, command.EmployeeId, AuditResults.Conflict, "InactiveRole", cancellationToken);
            throw ProblemExceptions.Conflict("Target role is inactive.");
        }

        var normalizedEmail = command.Email?.Trim().ToLowerInvariant();
        Guid? employeeId = command.EmployeeId;

        if (employeeId.HasValue)
        {
            var employee = await _dbContext.Employees
                .SingleOrDefaultAsync(x => x.Id == employeeId.Value && x.DeletedAtUtc == null, cancellationToken);

            if (employee is null)
            {
                await WriteUserCreatedAuditAsync(null, role.Id, employeeId, AuditResults.NotFound, "EmployeeNotFound", cancellationToken);
                throw ProblemExceptions.NotFound("Employee was not found.");
            }

            normalizedEmail = employee.Email.Trim().ToLowerInvariant();
        }

        if ((role.Code == RoleCodes.Manager || role.Code == RoleCodes.Developer) && !employeeId.HasValue)
        {
            await WriteUserCreatedAuditAsync(null, role.Id, null, AuditResults.Conflict, "EmployeeLinkRequired", cancellationToken);
            throw ProblemExceptions.Conflict("Manager and Developer users require a linked employee.");
        }

        if ((role.Code == RoleCodes.Admin || role.Code == RoleCodes.HR) && !employeeId.HasValue && string.IsNullOrWhiteSpace(normalizedEmail))
        {
            await WriteUserCreatedAuditAsync(null, role.Id, null, AuditResults.Rejected, "EmailRequired", cancellationToken);
            throw new AppProblemException(StatusCodes.Status400BadRequest, "Invalid user request", "email is required when employeeId is omitted.");
        }

        if ((role.Code != RoleCodes.Admin && role.Code != RoleCodes.HR) && !employeeId.HasValue)
        {
            await WriteUserCreatedAuditAsync(null, role.Id, null, AuditResults.Conflict, "RoleLinkRuleViolation", cancellationToken);
            throw ProblemExceptions.Conflict("The selected role requires a linked employee.");
        }

        if (employeeId.HasValue)
        {
            var employeeAlreadyLinked = await _dbContext.Users
                .AnyAsync(x => x.EmployeeId == employeeId.Value, cancellationToken);

            if (employeeAlreadyLinked)
            {
                await WriteUserCreatedAuditAsync(null, role.Id, employeeId, AuditResults.Conflict, "EmployeeAlreadyLinked", cancellationToken);
                throw ProblemExceptions.Conflict("Employee is already linked to another user.");
            }
        }

        if (await _dbContext.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
        {
            await WriteUserCreatedAuditAsync(null, role.Id, employeeId, AuditResults.Conflict, "DuplicateEmail", cancellationToken);
            throw ProblemExceptions.Conflict("A user with the same email already exists.");
        }

        var temporaryPassword = GenerateTemporaryPassword();
        var nowUtc = _clock.UtcNow;
        var expiresAtUtc = nowUtc.AddHours(24);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail!,
            RoleId = role.Id,
            Role = role,
            EmployeeId = employeeId,
            IsActive = true,
            MustChangePassword = true,
            TemporaryPasswordExpiresAtUtc = expiresAtUtc,
            LastTemporaryPasswordIssuedAtUtc = nowUtc
        };

        user.PasswordHash = _passwordHashingService.HashPassword(user, temporaryPassword);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var loginUrl = new Uri(new Uri(_frontendOptions.BaseUrl.TrimEnd('/') + "/"), "login").ToString();
        var emailMessage = _userAccessEmailComposer.ComposeTemporaryPasswordInvite(
            new TemporaryPasswordEmailModel(
                new EmailRecipient(user.Email),
                loginUrl,
                temporaryPassword,
                expiresAtUtc,
                role.Name));

        await _emailSender.SendAsync(emailMessage, cancellationToken);

        await _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.UserCreated,
                AuditEntityTypes.User,
                AuditResults.Success,
                user.Id,
                new
                {
                    role.Id,
                    role.Code,
                    user.EmployeeId,
                    user.Email
                }),
            cancellationToken);

        await _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.TemporaryPasswordIssued,
                AuditEntityTypes.User,
                AuditResults.Success,
                user.Id,
                new
                {
                    ExpiresAtUtc = expiresAtUtc,
                    user.Email
                }),
            cancellationToken);

        return new Result(
            user.Id,
            user.Email,
            role.Id,
            role.Code,
            user.EmployeeId,
            user.MustChangePassword,
            expiresAtUtc);
    }

    public async Task<ResendResult> ResendTemporaryPasswordAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null || user.Role is null)
        {
            await WriteTemporaryPasswordResentAuditAsync(userId, AuditResults.NotFound, "UserNotFound", cancellationToken);
            throw ProblemExceptions.NotFound("User was not found.");
        }

        if (!user.IsActive || !user.MustChangePassword || !user.TemporaryPasswordExpiresAtUtc.HasValue)
        {
            await WriteTemporaryPasswordResentAuditAsync(user.Id, AuditResults.Rejected, "NotInOnboardingState", cancellationToken);
            throw new AppProblemException(StatusCodes.Status400BadRequest, "User is not in onboarding state.");
        }

        var temporaryPassword = GenerateTemporaryPassword();
        var nowUtc = _clock.UtcNow;
        var expiresAtUtc = nowUtc.AddHours(24);

        user.PasswordHash = _passwordHashingService.HashPassword(user, temporaryPassword);
        user.MustChangePassword = true;
        user.TemporaryPasswordExpiresAtUtc = expiresAtUtc;
        user.LastTemporaryPasswordIssuedAtUtc = nowUtc;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var loginUrl = new Uri(new Uri(_frontendOptions.BaseUrl.TrimEnd('/') + "/"), "login").ToString();
        var emailMessage = _userAccessEmailComposer.ComposeTemporaryPasswordResend(
            new TemporaryPasswordEmailModel(
                new EmailRecipient(user.Email),
                loginUrl,
                temporaryPassword,
                expiresAtUtc,
                user.Role.Name));

        var sessionsRevoked = await _userSessionAuthenticationService.RevokeActiveSessionsAsync(user.Id, "TemporaryPasswordResend", cancellationToken);
        await _emailSender.SendAsync(emailMessage, cancellationToken);

        await _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.TemporaryPasswordResent,
                AuditEntityTypes.User,
                AuditResults.Success,
                user.Id,
                new
                {
                    ExpiresAtUtc = expiresAtUtc,
                    SessionsRevoked = sessionsRevoked,
                    user.Email
                }),
            cancellationToken);

        return new ResendResult(user.Id, expiresAtUtc);
    }

    private Task WriteUserCreatedAuditAsync(
        Guid? userId,
        Guid roleId,
        Guid? employeeId,
        string result,
        string reason,
        CancellationToken cancellationToken)
    {
        return _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.UserCreated,
                AuditEntityTypes.User,
                result,
                userId,
                new
                {
                    RoleId = roleId,
                    EmployeeId = employeeId,
                    Reason = reason
                }),
            cancellationToken);
    }

    private Task WriteTemporaryPasswordResentAuditAsync(
        Guid userId,
        string result,
        string reason,
        CancellationToken cancellationToken)
    {
        return _auditLogService.WriteAsync(
            new AuditWriteEntry(
                AuditActionTypes.TemporaryPasswordResent,
                AuditEntityTypes.User,
                result,
                userId,
                new { Reason = reason }),
            cancellationToken);
    }

    private static string GenerateTemporaryPassword()
    {
        var characters = new char[18];
        characters[0] = GetRandomCharacter(Uppercase);
        characters[1] = GetRandomCharacter(Lowercase);
        characters[2] = GetRandomCharacter(Digits);
        characters[3] = GetRandomCharacter(Symbols);

        for (var i = 4; i < characters.Length; i++)
        {
            characters[i] = GetRandomCharacter(AllCharacters);
        }

        Shuffle(characters);
        return new string(characters);
    }

    private static char GetRandomCharacter(IReadOnlyList<char> source)
    {
        return source[RandomNumberGenerator.GetInt32(source.Count)];
    }

    private static void Shuffle(Span<char> value)
    {
        for (var index = value.Length - 1; index > 0; index--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
            (value[index], value[swapIndex]) = (value[swapIndex], value[index]);
        }
    }
}
