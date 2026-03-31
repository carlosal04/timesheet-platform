using TimeSheet.Modules.EmploymentManagement.Application.Authentication.Login;
using ChangePasswordResult = TimeSheet.Modules.EmploymentManagement.Application.Authentication.ChangePassword.Result;
using RenewResult = TimeSheet.Modules.EmploymentManagement.Application.Authentication.Renew.Result;

namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;

public interface IUserSessionAuthenticationService
{
    Task<Result> LoginAsync(string email, string password, CancellationToken cancellationToken);

    Task<ChangePasswordResult?> ChangePasswordAsync(
        Guid userId,
        Guid sessionId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken);

    Task<RenewResult?> RenewAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);

    Task LogoutAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<int> RevokeActiveSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken);
}
