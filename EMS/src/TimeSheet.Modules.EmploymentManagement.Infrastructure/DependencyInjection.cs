using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Addresses;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Audit;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Email;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Employees;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Roles;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Users;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication.Configuration;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Authentication;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Auditing;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Configuration;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Email;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Email.Configuration;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Employees;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Initialization;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Roles;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Users;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEmploymentManagementInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<BootstrapAdminOptions>(configuration.GetSection(BootstrapAdminOptions.SectionName));
        services.AddOptions<FrontendOptions>()
            .Bind(configuration.GetSection(FrontendOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<FrontendOptions>, FrontendOptionsValidator>();
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();

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
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IPasswordHashingService, PasswordHashingService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IUserAccessEmailComposer, UserAccessEmailComposer>();
        services.AddScoped<IUserSessionAuthenticationService, UserSessionAuthenticationService>();
        services.AddScoped<ISessionReadService, SessionReadService>();
        services.AddScoped<ISessionValidator, SessionValidator>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IEmployeeReadService, EmployeeReadService>();
        services.AddScoped<IEmployeeWriteService, EmployeeWriteService>();
        services.AddScoped<IEmployeeAddressService, EmployeeAddressService>();
        services.AddScoped<IRoleReadService, RoleReadService>();
        services.AddScoped<IUserProvisioningService, UserProvisioningService>();
        services.AddScoped<IUserReadService, UserReadService>();
        services.AddScoped<IUserRoleService, UserRoleService>();
        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}
