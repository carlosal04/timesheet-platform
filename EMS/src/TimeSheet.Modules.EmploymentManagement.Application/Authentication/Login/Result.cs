using TimeSheet.Modules.EmploymentManagement.Domain.Security;

namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication.Login;

public sealed class Result
{
    private Result(bool succeeded, string? errorCode, User? user, UserSession? session, string? roleCode)
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

    public static Result Success(User user, UserSession session, string roleCode)
    {
        return new Result(true, null, user, session, roleCode);
    }

    public static Result Failure(string errorCode)
    {
        return new Result(false, errorCode, null, null, null);
    }
}
