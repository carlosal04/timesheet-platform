namespace TimeSheet.Modules.EmploymentManagement.Application.Addresses.SetPrimary;

public sealed record Command(Guid EmployeeId, Guid AddressId);
