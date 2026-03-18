namespace TimeSheet.Modules.EmploymentManagement.Application.Employees.Create;

public sealed record Address(
    string AddressType,
    bool IsPrimary,
    string Line1,
    string? Line2,
    string City,
    string State,
    string ZipCode,
    string CountryCode);
