namespace TimeSheet.Modules.EmploymentManagement.Application.Employees.List;

public sealed record Result(IReadOnlyList<Item> Items, int Page, int PageSize, int TotalCount);
