using TimeSheet.Modules.EmploymentManagement.Domain.Security;

namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication;

public sealed class SessionValidationResult
{
    private SessionValidationResult(bool succeeded, User? user, string? roleCode)
    {
        Succeeded = succeeded;
        User = user;
        RoleCode = roleCode;
    }

    public bool Succeeded { get; }

    public User? User { get; }

    public string? RoleCode { get; }

    public static SessionValidationResult Success(User user, string roleCode)
    {
        return new SessionValidationResult(true, user, roleCode);
    }

    public static SessionValidationResult Failure()
    {
        return new SessionValidationResult(false, null, null);
    }
}
