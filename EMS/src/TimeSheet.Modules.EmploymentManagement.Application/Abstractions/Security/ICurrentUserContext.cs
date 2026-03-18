namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    Guid? EmployeeId { get; }

    Guid? SessionId { get; }

    bool IsInRole(string roleCode);
}
