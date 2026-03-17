namespace TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;

public sealed record AntiforgeryResponse(string HeaderName, string RequestToken);
