using Microsoft.AspNetCore.Mvc;

namespace TimeSheet.Modules.EmploymentManagement.Api.Authentication;

public sealed class LoginRateLimitFilter : IEndpointFilter
{
    private readonly LoginRateLimiter _loginRateLimiter;

    public LoginRateLimitFilter(LoginRateLimiter loginRateLimiter)
    {
        _loginRateLimiter = loginRateLimiter;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var lease = await _loginRateLimiter.AcquireAsync(context.HttpContext, context.HttpContext.RequestAborted);
        if (!lease.IsAcquired)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "Too many login attempts");
        }

        return await next(context);
    }
}
