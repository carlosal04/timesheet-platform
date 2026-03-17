namespace TimeSheet.Modules.EmploymentManagement.Application.Employees;

public sealed record EmployeeDetailView(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateOnly DateOfBirth,
    DateOnly HireDate,
    string Status,
    IReadOnlyList<EmployeeAddressView> Addresses);
