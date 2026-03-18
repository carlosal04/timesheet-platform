namespace TimeSheet.Modules.EmploymentManagement.Application.Roles.List;

public sealed record Item(
    Guid Id,
    string Code,
    string Name,
    bool IsActive,
    bool IsSystem);
