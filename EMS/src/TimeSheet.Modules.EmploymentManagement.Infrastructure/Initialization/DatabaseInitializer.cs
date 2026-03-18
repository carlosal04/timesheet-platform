using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication.Configuration;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Initialization;

public sealed class DatabaseInitializer
{
    private readonly EmploymentManagementDbContext _dbContext;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly BootstrapAdminOptions _bootstrapAdminOptions;

    public DatabaseInitializer(
        EmploymentManagementDbContext dbContext,
        IPasswordHashingService passwordHashingService,
        IOptions<BootstrapAdminOptions> bootstrapAdminOptions)
    {
        _dbContext = dbContext;
        _passwordHashingService = passwordHashingService;
        _bootstrapAdminOptions = bootstrapAdminOptions.Value;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_dbContext.Database.IsRelational())
        {
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (!await _dbContext.Roles.AnyAsync(cancellationToken))
        {
            _dbContext.Roles.AddRange(
                new Role
                {
                    Id = Guid.NewGuid(),
                    Code = RoleCodes.Admin,
                    Name = RoleCodes.Admin,
                    IsActive = true
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Code = RoleCodes.Basic,
                    Name = RoleCodes.Basic,
                    IsActive = true
                });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(_bootstrapAdminOptions.Email) || string.IsNullOrWhiteSpace(_bootstrapAdminOptions.Password))
        {
            return;
        }

        var adminEmail = _bootstrapAdminOptions.Email.Trim().ToLowerInvariant();
        var adminRole = await _dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Admin, cancellationToken);
        var existingAdmin = await _dbContext.Users.SingleOrDefaultAsync(x => x.Email == adminEmail, cancellationToken);
        if (existingAdmin is not null)
        {
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = adminEmail,
            RoleId = adminRole.Id,
            IsActive = true
        };

        user.PasswordHash = _passwordHashingService.HashPassword(user, _bootstrapAdminOptions.Password);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
