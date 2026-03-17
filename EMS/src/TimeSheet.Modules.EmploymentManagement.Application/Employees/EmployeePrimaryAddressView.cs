namespace TimeSheet.Modules.EmploymentManagement.Application.Employees;

public sealed record EmployeePrimaryAddressView(
    Guid Id,
    string AddressType,
    bool IsPrimary,
    string Line1,
    string? Line2,
    string City,
    string State,
    string ZipCode,
    string CountryCode);
