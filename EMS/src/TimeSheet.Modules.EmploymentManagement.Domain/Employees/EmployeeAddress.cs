namespace TimeSheet.Modules.EmploymentManagement.Domain.Employees;

public sealed class EmployeeAddress
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public string AddressType { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public string Line1 { get; set; } = string.Empty;

    public string? Line2 { get; set; }

    public string City { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string ZipCode { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public bool IsDeleted => DeletedAtUtc.HasValue;
}
