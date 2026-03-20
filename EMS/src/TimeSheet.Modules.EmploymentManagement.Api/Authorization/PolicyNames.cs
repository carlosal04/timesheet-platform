namespace TimeSheet.Modules.EmploymentManagement.Api.Authorization;

public static class PolicyNames
{
    public const string AuthenticatedUser = "AuthenticatedUser";
    public const string AdminOnly = "AdminOnly";
    public const string EmployeeRead = "EmployeeRead";
    public const string EmployeeWrite = "EmployeeWrite";
    public const string EmployeeDelete = "EmployeeDelete";
    public const string AddressRead = "AddressRead";
    public const string AddressWrite = "AddressWrite";
    public const string AddressDeleteAny = "AddressDeleteAny";
    public const string AddressPrimaryManageAny = "AddressPrimaryManageAny";
    public const string OwnAddressDelete = "OwnAddressDelete";
    public const string OwnAddressPrimaryManage = "OwnAddressPrimaryManage";
    public const string RoleRead = "RoleRead";
    public const string UserRead = "UserRead";
    public const string UserRoleAssign = "UserRoleAssign";
    public const string AuditLogRead = "AuditLogRead";
}
