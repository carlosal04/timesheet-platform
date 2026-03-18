namespace TimeSheet.Modules.EmploymentManagement.Application.Employees;

public sealed record ListEmployeesQuery(
    int Page,
    int PageSize,
    string? Name,
    string? Status,
    DateOnly? HireDateFrom,
    DateOnly? HireDateTo,
    bool IncludeDeleted,
    bool IncludePrimaryAddress);
