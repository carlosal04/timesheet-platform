using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace TimeSheet.Modules.EmploymentManagement.Api.Infrastructure;

public sealed class AuthenticatedAntiforgeryMiddleware
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Options,
        HttpMethods.Trace
    };

    private readonly RequestDelegate _next;

    public AuthenticatedAntiforgeryMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        if (ShouldValidate(context))
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                var problemDetails = new ProblemDetails
                {
                    Title = "Invalid anti-forgery token",
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://httpstatuses.com/400"
                };

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
                return;
            }
        }

        await _next(context);
    }

    private static bool ShouldValidate(HttpContext context)
    {
        if (SafeMethods.Contains(context.Request.Method))
        {
            return false;
        }

        if (context.Request.Path.Equals("/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return context.User.Identity?.IsAuthenticated == true;
    }
}
