using Microsoft.Extensions.Options;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Configuration;

public sealed class FrontendOptionsValidator : IValidateOptions<FrontendOptions>
{
    public ValidateOptionsResult Validate(string? name, FrontendOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            return ValidateOptionsResult.Fail("Frontend:BaseUrl is required.");
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return ValidateOptionsResult.Fail("Frontend:BaseUrl must be an absolute http or https URL.");
        }

        return ValidateOptionsResult.Success;
    }
}
