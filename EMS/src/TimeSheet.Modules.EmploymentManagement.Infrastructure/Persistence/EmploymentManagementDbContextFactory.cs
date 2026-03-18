using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

public sealed class EmploymentManagementDbContextFactory : IDesignTimeDbContextFactory<EmploymentManagementDbContext>
{
    public EmploymentManagementDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<EmploymentManagementDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new EmploymentManagementDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString()
    {
        var directConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__EmploymentManagement");
        if (IsUsableValue(directConnectionString))
        {
            return directConnectionString!;
        }

        var host = GetRequiredEnvironmentVariable("EMS_DB_HOST");
        var port = GetRequiredEnvironmentVariable("EMS_DB_PORT");
        var database = GetRequiredEnvironmentVariable("EMS_DB_NAME");
        var username = GetRequiredEnvironmentVariable("EMS_DB_USER");
        var password = GetRequiredEnvironmentVariable("EMS_DB_PASSWORD");

        return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
    }

    private static string GetRequiredEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (IsUsableValue(value))
        {
            return value!;
        }

        throw new InvalidOperationException(
            "Design-time database configuration is missing. Set ConnectionStrings__EmploymentManagement " +
            "or provide EMS_DB_HOST, EMS_DB_PORT, EMS_DB_NAME, EMS_DB_USER, and EMS_DB_PASSWORD with non-placeholder values.");
    }

    private static bool IsUsableValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return !value.Contains("SET_LOCAL_", StringComparison.OrdinalIgnoreCase);
    }
}
