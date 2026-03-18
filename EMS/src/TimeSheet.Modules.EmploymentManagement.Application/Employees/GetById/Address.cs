namespace TimeSheet.Modules.EmploymentManagement.Application.Employees.GetById;

public sealed record Address(
    Guid Id,
    string AddressType,
    bool IsPrimary,
    string Line1,
    string? Line2,
    string City,
    string State,
    string ZipCode,
    string CountryCode);
