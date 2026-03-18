namespace TimeSheet.Modules.EmploymentManagement.Application.Addresses.Update;

public sealed record Command(
    Guid EmployeeId,
    Guid AddressId,
    string AddressType,
    bool IsPrimary,
    string Line1,
    string? Line2,
    string City,
    string State,
    string ZipCode,
    string CountryCode);
