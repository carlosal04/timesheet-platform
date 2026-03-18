using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Authentication;

public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId => ParseGuid(Principal?.FindFirstValue(ClaimTypes.NameIdentifier));

    public Guid? EmployeeId => ParseGuid(Principal?.FindFirstValue(CustomClaimTypes.EmployeeId));

    public Guid? SessionId => ParseGuid(Principal?.FindFirstValue(CustomClaimTypes.SessionId));

    public bool IsInRole(string roleCode)
    {
        return Principal?.IsInRole(roleCode) == true;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    private static Guid? ParseGuid(string? value)
    {
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}
