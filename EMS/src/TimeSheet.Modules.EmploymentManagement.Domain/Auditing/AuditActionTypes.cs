namespace TimeSheet.Modules.EmploymentManagement.Domain.Auditing;

public static class AuditActionTypes
{
    public const string LoginSucceeded = "LoginSucceeded";
    public const string LoginFailed = "LoginFailed";
    public const string LogoutSucceeded = "LogoutSucceeded";
    public const string SessionCreated = "SessionCreated";
    public const string SessionRenewed = "SessionRenewed";
    public const string SessionRevoked = "SessionRevoked";
    public const string AccessDenied = "AccessDenied";
    public const string AccountLockedOut = "AccountLockedOut";
    public const string PasswordChanged = "PasswordChanged";
    public const string UserCreated = "UserCreated";
    public const string TemporaryPasswordIssued = "TemporaryPasswordIssued";
    public const string TemporaryPasswordResent = "TemporaryPasswordResent";
    public const string PasswordResetRequested = "PasswordResetRequested";
    public const string UserRoleAssigned = "UserRoleAssigned";
    public const string RoleAssignmentRejected = "RoleAssignmentRejected";
    public const string EmployeeCreated = "EmployeeCreated";
    public const string EmployeeRead = "EmployeeRead";
    public const string EmployeeListRead = "EmployeeListRead";
    public const string EmployeeUpdated = "EmployeeUpdated";
    public const string EmployeeSoftDeleted = "EmployeeSoftDeleted";
    public const string AddressCreated = "AddressCreated";
    public const string AddressRead = "AddressRead";
    public const string AddressListRead = "AddressListRead";
    public const string AddressUpdated = "AddressUpdated";
    public const string AddressPrimaryChanged = "AddressPrimaryChanged";
    public const string AddressSoftDeleted = "AddressSoftDeleted";
    public const string AuditLogRead = "AuditLogRead";
    public const string ConfigurationError = "ConfigurationError";
    public const string UnhandledException = "UnhandledException";
}
