namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
