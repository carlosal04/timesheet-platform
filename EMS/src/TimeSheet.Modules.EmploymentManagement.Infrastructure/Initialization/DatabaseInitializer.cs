using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication.Configuration;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Initialization;

public sealed class DatabaseInitializer
{
    private const string AdminRoleName = "Administrator";
    private const string BasicRoleName = "Basic User";

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

        var adminRole = await EnsureRoleAsync(RoleCodes.Admin, AdminRoleName, cancellationToken);
        await EnsureRoleAsync(RoleCodes.Basic, BasicRoleName, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(_bootstrapAdminOptions.Email) || string.IsNullOrWhiteSpace(_bootstrapAdminOptions.Password))
        {
            return;
        }

        var adminEmail = _bootstrapAdminOptions.Email.Trim().ToLowerInvariant();
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

    private async Task<Role> EnsureRoleAsync(string code, string name, CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles.SingleOrDefaultAsync(x => x.Code == code, cancellationToken);
        if (role is null)
        {
            role = new Role
            {
                Id = Guid.NewGuid(),
                Code = code
            };

            _dbContext.Roles.Add(role);
        }

        role.Name = name;
        role.IsActive = true;
        role.IsSystem = true;

        return role;
    }
}
