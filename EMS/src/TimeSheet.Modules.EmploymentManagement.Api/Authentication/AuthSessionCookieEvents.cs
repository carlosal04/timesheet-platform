using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication;

namespace TimeSheet.Modules.EmploymentManagement.Api.Authentication;

public sealed class AuthSessionCookieEvents : CookieAuthenticationEvents
{
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        if (principal is null)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
            return;
        }

        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionIdValue = principal.FindFirstValue("session_id");
        if (!Guid.TryParse(userIdValue, out var userId) || !Guid.TryParse(sessionIdValue, out var sessionId))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
            return;
        }

        var validator = context.HttpContext.RequestServices.GetRequiredService<ISessionValidator>();
        var result = await validator.ValidateAsync(userId, sessionId, context.HttpContext.RequestAborted);
        if (!result.Succeeded)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
            return;
        }

        var identity = principal.Identity as ClaimsIdentity;
        if (identity is null)
        {
            return;
        }

        var existingRole = identity.FindFirst(ClaimTypes.Role);
        if (existingRole?.Value != result.RoleCode)
        {
            if (existingRole is not null)
            {
                identity.RemoveClaim(existingRole);
            }

            identity.AddClaim(new Claim(ClaimTypes.Role, result.RoleCode!));
        }
    }
}
