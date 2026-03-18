namespace TimeSheet.Modules.EmploymentManagement.Application.Employees.GetById;

public sealed record Employee(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateOnly DateOfBirth,
    DateOnly HireDate,
    string Status,
    IReadOnlyList<Address> Addresses);
