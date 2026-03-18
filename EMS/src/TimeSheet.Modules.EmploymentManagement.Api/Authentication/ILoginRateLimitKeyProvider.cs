namespace TimeSheet.Modules.EmploymentManagement.Api.Authentication;

public interface ILoginRateLimitKeyProvider
{
    string GetPartitionKey(HttpContext httpContext);
}
