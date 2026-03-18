namespace TimeSheet.Modules.EmploymentManagement.Application.Employees.Create;

public sealed record Command(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateOnly DateOfBirth,
    DateOnly HireDate,
    string Status,
    IReadOnlyList<Address>? Addresses);
