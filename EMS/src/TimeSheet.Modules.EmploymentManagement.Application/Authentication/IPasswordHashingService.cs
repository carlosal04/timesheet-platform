using TimeSheet.Modules.EmploymentManagement.Domain.Security;

namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication;

public interface IPasswordHashingService
{
    string HashPassword(User user, string password);

    bool VerifyPassword(User user, string password);
}
