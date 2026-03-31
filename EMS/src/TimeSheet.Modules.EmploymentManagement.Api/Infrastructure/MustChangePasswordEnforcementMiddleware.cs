using Microsoft.AspNetCore.Mvc;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;

namespace TimeSheet.Modules.EmploymentManagement.Api.Infrastructure;

public sealed class MustChangePasswordEnforcementMiddleware
{
    private readonly RequestDelegate _next;

    public MustChangePasswordEnforcementMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldBlock(context))
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Password change required",
                Detail = "Change your password before accessing other EMS features.",
                Status = StatusCodes.Status403Forbidden,
                Type = "https://httpstatuses.com/403"
            };

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
            return;
        }

        await _next(context);
    }

    private static bool ShouldBlock(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var mustChangePasswordValue = context.User.FindFirst(CustomClaimTypes.MustChangePassword)?.Value;
        if (!bool.TryParse(mustChangePasswordValue, out var mustChangePassword) || !mustChangePassword)
        {
            return false;
        }

        return !IsAllowedPath(context.Request.Path);
    }

    private static bool IsAllowedPath(PathString path)
    {
        return path.Equals("/auth/logout", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/auth/session", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/auth/antiforgery", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/auth/renew", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/auth/change-password", StringComparison.OrdinalIgnoreCase);
    }
}
