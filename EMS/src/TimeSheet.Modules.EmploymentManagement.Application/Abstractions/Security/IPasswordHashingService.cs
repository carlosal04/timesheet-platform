using TimeSheet.Modules.EmploymentManagement.Domain.Security;

namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;

public interface IPasswordHashingService
{
    string HashPassword(User user, string password);

    bool VerifyPassword(User user, string password);
}
