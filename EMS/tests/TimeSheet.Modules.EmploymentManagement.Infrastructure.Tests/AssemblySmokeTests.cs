using TimeSheet.Modules.EmploymentManagement.Infrastructure;

namespace TimeSheet.Modules.EmploymentManagement.Infrastructure.Tests;

public class AssemblySmokeTests
{
    [Fact]
    public void InfrastructureAssemblyMarker_IsAccessible()
    {
        _ = new AssemblyMarker();
    }
}
