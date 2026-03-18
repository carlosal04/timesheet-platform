namespace TimeSheet.Modules.EmploymentManagement.Application.Employees.List;

public sealed record Query(
    int Page,
    int PageSize,
    string? Name,
    string? Status,
    DateOnly? HireDateFrom,
    DateOnly? HireDateTo,
    bool IncludeDeleted,
    bool IncludePrimaryAddress);
