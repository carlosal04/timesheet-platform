using TimeSheet.Modules.EmploymentManagement.Domain.Security;

namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication.Login;

public sealed class Result
{
    private Result(
        bool succeeded,
        string? errorCode,
        User? user,
        UserSession? session,
        string? roleCode,
        Guid? employeeId,
        bool? mustChangePassword)
    {
        Succeeded = succeeded;
        ErrorCode = errorCode;
        User = user;
        Session = session;
        RoleCode = roleCode;
        EmployeeId = employeeId;
        MustChangePassword = mustChangePassword;
    }

    public bool Succeeded { get; }

    public string? ErrorCode { get; }

    public User? User { get; }

    public UserSession? Session { get; }

    public string? RoleCode { get; }

    public Guid? EmployeeId { get; }

    public bool? MustChangePassword { get; }

    public static Result Success(User user, UserSession session, string roleCode)
    {
        return new Result(true, null, user, session, roleCode, user.EmployeeId, user.MustChangePassword);
    }

    public static Result Failure(string errorCode)
    {
        return new Result(false, errorCode, null, null, null, null, null);
    }
}
