using TimeSheet.Modules.EmploymentManagement.Application.Authentication.Login;

namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;

public interface IUserSessionAuthenticationService
{
    Task<Result> LoginAsync(string email, string password, CancellationToken cancellationToken);

    Task LogoutAsync(Guid sessionId, CancellationToken cancellationToken);
}
