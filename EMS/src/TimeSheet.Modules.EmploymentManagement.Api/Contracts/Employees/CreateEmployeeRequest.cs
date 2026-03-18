using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Addresses;

namespace TimeSheet.Modules.EmploymentManagement.Api.Contracts.Employees;

public sealed record CreateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateOnly DateOfBirth,
    DateOnly HireDate,
    string Status,
    IReadOnlyList<UpsertAddressRequest>? Addresses);
