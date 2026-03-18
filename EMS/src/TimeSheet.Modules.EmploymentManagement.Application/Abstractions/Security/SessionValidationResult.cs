using TimeSheet.Modules.EmploymentManagement.Domain.Security;

namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;

public sealed class SessionValidationResult
{
    private SessionValidationResult(bool succeeded, User? user, string? roleCode, Guid? employeeId)
    {
        Succeeded = succeeded;
        User = user;
        RoleCode = roleCode;
        EmployeeId = employeeId;
    }

    public bool Succeeded { get; }

    public User? User { get; }

    public string? RoleCode { get; }

    public Guid? EmployeeId { get; }

    public static SessionValidationResult Success(User user, string roleCode)
    {
        return new SessionValidationResult(true, user, roleCode, user.EmployeeId);
    }

    public static SessionValidationResult Failure()
    {
        return new SessionValidationResult(false, null, null, null);
    }
}
