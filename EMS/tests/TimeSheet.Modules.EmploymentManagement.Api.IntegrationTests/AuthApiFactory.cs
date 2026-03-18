using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Initialization;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public class AuthApiFactory : Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _databaseRoot = new();
    private readonly string _databaseName = $"ems-auth-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(GetConfigurationOverrides());
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<EmploymentManagementDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<EmploymentManagementDbContext>>();
            services.RemoveAll<EmploymentManagementDbContext>();

            services.AddDataProtection()
                .UseEphemeralDataProtectionProvider();

            services.AddDbContext<EmploymentManagementDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName, _databaseRoot);
            });
        });
    }

    protected virtual IDictionary<string, string?> GetConfigurationOverrides()
    {
        return new Dictionary<string, string?>
        {
            ["BootstrapAdmin:Email"] = "admin@example.com",
            ["BootstrapAdmin:Password"] = "P@ssw0rd123!",
            ["BootstrapAdmin:Name"] = "Test Admin",
            ["RateLimiting:Authentication:PermitLimit"] = "1000",
            ["RateLimiting:Authentication:WindowMinutes"] = "1",
            ["RateLimiting:Authentication:QueueLimit"] = "0"
        };
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EmploymentManagementDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync(CancellationToken.None);
    }

    public async Task SeedAsync(Func<EmploymentManagementDbContext, Task> seed)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EmploymentManagementDbContext>();
        await seed(dbContext);
        await dbContext.SaveChangesAsync();
    }

    public async Task ExecuteScopedAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider);
    }
}
