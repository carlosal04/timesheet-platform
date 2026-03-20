namespace TimeSheet.Modules.EmploymentManagement.Application.Users.List;

public sealed record Query(
    int Page,
    int PageSize,
    string? Email,
    string? RoleCode,
    bool IncludeInactive);
