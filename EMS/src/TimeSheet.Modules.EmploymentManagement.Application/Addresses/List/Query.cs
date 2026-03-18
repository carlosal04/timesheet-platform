namespace TimeSheet.Modules.EmploymentManagement.Application.Addresses.List;

public sealed record Query(Guid EmployeeId, bool IncludeDeleted = false);
