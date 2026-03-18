namespace TimeSheet.Modules.EmploymentManagement.Api.Contracts.Employees;

public sealed record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateOnly DateOfBirth,
    DateOnly HireDate,
    string Status);
