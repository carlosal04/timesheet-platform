using TimeSheet.Modules.EmploymentManagement.Domain;

namespace TimeSheet.Modules.EmploymentManagement.Domain.Tests;

public class AssemblySmokeTests
{
    [Fact]
    public void DomainAssemblyMarker_IsAccessible()
    {
        _ = new AssemblyMarker();
    }
}
