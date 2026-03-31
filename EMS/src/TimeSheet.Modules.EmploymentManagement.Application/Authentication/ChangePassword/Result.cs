namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication.ChangePassword;

public sealed class Result
{
    private Result(
        bool succeeded,
        string? errorCode,
        Guid? userId,
        string? email,
        string? roleCode,
        Guid? employeeId,
        Guid? sessionId,
        DateTimeOffset? expiresAtUtc)
    {
        Succeeded = succeeded;
        ErrorCode = errorCode;
        UserId = userId;
        Email = email;
        RoleCode = roleCode;
        EmployeeId = employeeId;
        SessionId = sessionId;
        ExpiresAtUtc = expiresAtUtc;
    }

    public bool Succeeded { get; }

    public string? ErrorCode { get; }

    public Guid? UserId { get; }

    public string? Email { get; }

    public string? RoleCode { get; }

    public Guid? EmployeeId { get; }

    public Guid? SessionId { get; }

    public DateTimeOffset? ExpiresAtUtc { get; }

    public static Result Success(
        Guid userId,
        string email,
        string roleCode,
        Guid? employeeId,
        Guid sessionId,
        DateTimeOffset expiresAtUtc)
    {
        return new Result(true, null, userId, email, roleCode, employeeId, sessionId, expiresAtUtc);
    }

    public static Result Failure(string errorCode)
    {
        return new Result(false, errorCode, null, null, null, null, null, null);
    }
}
