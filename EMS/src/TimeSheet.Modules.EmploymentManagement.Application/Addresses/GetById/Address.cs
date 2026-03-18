namespace TimeSheet.Modules.EmploymentManagement.Application.Addresses.GetById;

public sealed record Address(
    Guid Id,
    Guid EmployeeId,
    string AddressType,
    bool IsPrimary,
    string Line1,
    string? Line2,
    string City,
    string State,
    string ZipCode,
    string CountryCode);
