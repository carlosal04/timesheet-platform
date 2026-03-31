namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;

public interface IPasswordResetService
{
    Task RequestAsync(string email, CancellationToken cancellationToken);

    Task<bool> ResetAsync(string token, string newPassword, CancellationToken cancellationToken);
}
