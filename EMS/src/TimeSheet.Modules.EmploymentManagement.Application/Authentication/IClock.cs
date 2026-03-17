namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
