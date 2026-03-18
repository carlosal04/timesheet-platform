namespace TimeSheet.Modules.EmploymentManagement.Application.Addresses.Delete;

public sealed record Command(Guid EmployeeId, Guid AddressId);
