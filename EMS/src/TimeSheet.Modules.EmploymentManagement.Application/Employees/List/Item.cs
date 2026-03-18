namespace TimeSheet.Modules.EmploymentManagement.Application.Employees.List;

public sealed record Item(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateOnly DateOfBirth,
    DateOnly HireDate,
    string Status,
    PrimaryAddress? PrimaryAddress);
