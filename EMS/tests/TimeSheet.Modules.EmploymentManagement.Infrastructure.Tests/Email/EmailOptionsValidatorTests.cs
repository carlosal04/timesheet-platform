using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Email;
using TimeSheet.Modules.EmploymentManagement.Infrastructure;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Email;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Email.Configuration;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Tests.Email;

public class EmailOptionsValidatorTests
{
    [Fact]
    public void Validate_WithValidOptions_ReturnsSuccess()
    {
        var validator = new EmailOptionsValidator();
        var options = new EmailOptions
        {
            FromAddress = "no-reply@ems.local",
            FromDisplayName = "Employment Management System",
            Smtp = new SmtpOptions
            {
                Host = "localhost",
                Port = 1025,
                SecurityMode = EmailSecurityMode.None
            }
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WithInvalidFromAddress_ReturnsFailure()
    {
        var validator = new EmailOptionsValidator();
        var options = new EmailOptions
        {
            FromAddress = "not-an-email",
            Smtp = new SmtpOptions
            {
                Host = "localhost",
                Port = 1025
            }
        };

        var result = validator.Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("FromAddress", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WithInvalidPort_ReturnsFailure()
    {
        var validator = new EmailOptionsValidator();
        var options = new EmailOptions
        {
            FromAddress = "no-reply@ems.local",
            Smtp = new SmtpOptions
            {
                Host = "localhost",
                Port = 70000
            }
        };

        var result = validator.Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("Port", StringComparison.Ordinal));
    }

    [Fact]
    public void AddEmploymentManagementInfrastructure_RegistersSmtpEmailSender()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:EmploymentManagement"] = "Host=localhost;Port=15432;Database=ems;Username=ems;Password=test",
                ["Authentication:CookieName"] = "ems.auth",
                ["Email:FromAddress"] = "no-reply@ems.local",
                ["Email:FromDisplayName"] = "Employment Management System",
                ["Email:Smtp:Host"] = "localhost",
                ["Email:Smtp:Port"] = "1025",
                ["Email:Smtp:SecurityMode"] = "None"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEmploymentManagementInfrastructure(configuration);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<EmailOptions>>().Value;

        Assert.IsType<SmtpEmailSender>(sender);
        Assert.Equal("no-reply@ems.local", options.FromAddress);
        Assert.Equal("localhost", options.Smtp.Host);
        Assert.Equal(1025, options.Smtp.Port);
    }
}
