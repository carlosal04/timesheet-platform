using TimeSheet.Modules.EmploymentManagement.Application;

namespace TimeSheet.Modules.EmploymentManagement.Application.Tests;

public class AssemblySmokeTests
{
    [Fact]
    public void ApplicationAssemblyMarker_IsAccessible()
    {
        _ = new AssemblyMarker();
    }
}
