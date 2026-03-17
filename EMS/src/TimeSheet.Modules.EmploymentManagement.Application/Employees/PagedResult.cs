namespace TimeSheet.Modules.EmploymentManagement.Application.Employees;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
