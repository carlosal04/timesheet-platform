using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Authentication;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
