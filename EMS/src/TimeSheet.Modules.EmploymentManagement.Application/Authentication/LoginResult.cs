using TimeSheet.Modules.EmploymentManagement.Domain.Security;

namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication;

public sealed class LoginResult
{
    private LoginResult(bool succeeded, string? errorCode, User? user, UserSession? session, string? roleCode)
    {
        Succeeded = succeeded;
        ErrorCode = errorCode;
        User = user;
        Session = session;
        RoleCode = roleCode;
    }

    public bool Succeeded { get; }

    public string? ErrorCode { get; }

    public User? User { get; }

    public UserSession? Session { get; }

    public string? RoleCode { get; }

    public static LoginResult Success(User user, UserSession session, string roleCode)
    {
        return new LoginResult(true, null, user, session, roleCode);
    }

    public static LoginResult Failure(string errorCode)
    {
        return new LoginResult(false, errorCode, null, null, null);
    }
}
