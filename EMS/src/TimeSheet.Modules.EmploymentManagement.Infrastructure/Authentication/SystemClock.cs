using TimeSheet.Modules.EmploymentManagement.Application.Authentication;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Authentication;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
