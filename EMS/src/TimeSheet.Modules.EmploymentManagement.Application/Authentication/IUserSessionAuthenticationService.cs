namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication;

public interface IUserSessionAuthenticationService
{
    Task<LoginResult> LoginAsync(string email, string password, CancellationToken cancellationToken);

    Task LogoutAsync(Guid sessionId, CancellationToken cancellationToken);
}
