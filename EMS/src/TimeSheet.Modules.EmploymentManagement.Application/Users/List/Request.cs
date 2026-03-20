namespace TimeSheet.Modules.EmploymentManagement.Application.Users.List;

public sealed record Request(
    int Page,
    int PageSize,
    string? Email,
    string? RoleCode,
    bool IncludeInactive);
