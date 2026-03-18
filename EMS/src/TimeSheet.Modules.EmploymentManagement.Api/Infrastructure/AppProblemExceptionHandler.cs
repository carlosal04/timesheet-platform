using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TimeSheet.Modules.EmploymentManagement.Application.Common.Exceptions;

namespace TimeSheet.Modules.EmploymentManagement.Api.Infrastructure;

public sealed class AppProblemExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not AppProblemException appProblemException)
        {
            return false;
        }

        var problemDetails = new ProblemDetails
        {
            Title = appProblemException.Title,
            Detail = appProblemException.Detail,
            Status = appProblemException.StatusCode,
            Type = $"https://httpstatuses.com/{appProblemException.StatusCode}"
        };

        httpContext.Response.StatusCode = appProblemException.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
