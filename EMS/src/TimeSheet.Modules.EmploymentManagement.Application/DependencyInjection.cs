using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace TimeSheet.Modules.EmploymentManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddEmploymentManagementApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<AssemblyMarker>();
        return services;
    }
}
