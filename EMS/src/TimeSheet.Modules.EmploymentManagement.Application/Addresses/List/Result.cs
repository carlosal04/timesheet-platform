namespace TimeSheet.Modules.EmploymentManagement.Application.Addresses.List;

public sealed record Result(Guid EmployeeId, IReadOnlyList<Item> Items);
