namespace TimeSheet.Modules.EmploymentManagement.Application.Addresses.GetById;

public sealed record Query(Guid EmployeeId, Guid AddressId);
