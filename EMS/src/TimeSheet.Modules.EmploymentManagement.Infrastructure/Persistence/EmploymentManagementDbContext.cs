using Microsoft.EntityFrameworkCore;
using TimeSheet.Modules.EmploymentManagement.Domain.Employees;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

public sealed class EmploymentManagementDbContext : DbContext
{
    public EmploymentManagementDbContext(DbContextOptions<EmploymentManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<User> Users => Set<User>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<EmployeeAddress> EmployeeAddresses => Set<EmployeeAddress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(2048).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.EmployeeId).IsUnique().HasFilter("\"EmployeeId\" IS NOT NULL");
            entity.HasOne(x => x.Role)
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("user_sessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RevokeReason).HasMaxLength(64);
            entity.HasIndex(x => new { x.UserId, x.RevokedAtUtc });
            entity.HasIndex(x => x.ExpiresAtUtc);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("employees");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasMany(x => x.Addresses)
                .WithOne(x => x.Employee)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeAddress>(entity =>
        {
            entity.ToTable("employee_addresses");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AddressType).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Line1).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Line2).HasMaxLength(200);
            entity.Property(x => x.City).HasMaxLength(100).IsRequired();
            entity.Property(x => x.State).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ZipCode).HasMaxLength(20).IsRequired();
            entity.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
            entity.HasIndex(x => new { x.EmployeeId, x.IsPrimary, x.DeletedAtUtc })
                .HasFilter("\"IsPrimary\" = TRUE AND \"DeletedAtUtc\" IS NULL")
                .IsUnique();
        });
    }
}
