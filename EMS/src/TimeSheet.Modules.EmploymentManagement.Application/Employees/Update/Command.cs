namespace TimeSheet.Modules.EmploymentManagement.Application.Employees.Update;

public sealed record Command(
    Guid EmployeeId,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateOnly DateOfBirth,
    DateOnly HireDate,
    string Status);
