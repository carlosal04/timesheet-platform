namespace TimeSheet.Modules.EmploymentManagement.Api.Contracts.Addresses;

public sealed record UpsertAddressRequest(
    string AddressType,
    bool IsPrimary,
    string Line1,
    string? Line2,
    string City,
    string State,
    string ZipCode,
    string CountryCode);
