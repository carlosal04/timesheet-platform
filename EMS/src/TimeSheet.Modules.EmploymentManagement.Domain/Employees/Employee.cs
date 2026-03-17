namespace TimeSheet.Modules.EmploymentManagement.Domain.Employees;

public sealed class Employee
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public DateOnly HireDate { get; set; }

    public string Status { get; set; } = EmployeeStatusCodes.Active;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public ICollection<EmployeeAddress> Addresses { get; set; } = new List<EmployeeAddress>();

    public bool IsDeleted => DeletedAtUtc.HasValue;
}
