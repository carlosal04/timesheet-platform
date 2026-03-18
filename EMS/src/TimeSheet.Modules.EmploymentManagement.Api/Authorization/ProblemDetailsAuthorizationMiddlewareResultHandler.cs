using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Audit;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;

namespace TimeSheet.Modules.EmploymentManagement.Api.Authorization;

public sealed class ProblemDetailsAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        if (authorizeResult.Forbidden)
        {
            var auditLogService = context.RequestServices.GetService<IAuditLogService>();
            if (auditLogService is not null)
            {
                await auditLogService.WriteAsync(
                    new AuditWriteEntry(
                        AuditActionTypes.AccessDenied,
                        AuditEntityTypes.Authentication,
                        AuditResults.Denied,
                        null,
                        new
                        {
                            Path = context.Request.Path.Value,
                            Policies = policy.Requirements.Select(requirement => requirement.GetType().Name).ToArray()
                        }),
                    context.RequestAborted);
            }
        }

        var statusCode = authorizeResult.Forbidden
            ? StatusCodes.Status403Forbidden
            : StatusCodes.Status401Unauthorized;

        var problemDetails = new ProblemDetails
        {
            Title = authorizeResult.Forbidden ? "Access denied" : "Authentication required",
            Status = statusCode,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
    }
}
