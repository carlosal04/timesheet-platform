namespace TimeSheet.Modules.EmploymentManagement.Domain.Security;

public sealed class Role
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
