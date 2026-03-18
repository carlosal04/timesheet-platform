namespace TimeSheet.Modules.EmploymentManagement.Application.Common.Exceptions;

public sealed class AppProblemException : Exception
{
    public AppProblemException(int statusCode, string title, string? detail = null)
        : base(detail ?? title)
    {
        StatusCode = statusCode;
        Title = title;
        Detail = detail;
    }

    public int StatusCode { get; }

    public string Title { get; }

    public string? Detail { get; }
}
