namespace TimeSheet.Modules.EmploymentManagement.Application.Common.Exceptions;

public static class ProblemExceptions
{
    public static AppProblemException NotFound(string title, string? detail = null)
    {
        return new AppProblemException(404, title, detail);
    }

    public static AppProblemException Forbidden(string title, string? detail = null)
    {
        return new AppProblemException(403, title, detail);
    }

    public static AppProblemException Conflict(string title, string? detail = null)
    {
        return new AppProblemException(409, title, detail);
    }
}
