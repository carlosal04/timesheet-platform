using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Employees;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication.Configuration;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Authentication;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Employees;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Initialization;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEmploymentManagementInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<BootstrapAdminOptions>(configuration.GetSection(BootstrapAdminOptions.SectionName));

        services.AddDbContext<EmploymentManagementDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("EmploymentManagement");
            options.UseNpgsql(connectionString);
        });

        services.AddHttpClient("ems-default")
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.Retry.UseJitter = false;
                options.Retry.Delay = TimeSpan.FromSeconds(2);
            });

        services.AddScoped<IClock, SystemClock>();
        services.AddScoped<IPasswordHashingService, PasswordHashingService>();
        services.AddScoped<IUserSessionAuthenticationService, UserSessionAuthenticationService>();
        services.AddScoped<ISessionValidator, SessionValidator>();
        services.AddScoped<IEmployeeReadService, EmployeeReadService>();
        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}
