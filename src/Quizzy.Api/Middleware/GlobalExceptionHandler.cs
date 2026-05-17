using Microsoft.AspNetCore.Diagnostics;

namespace Quizzy.Api.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception: {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        var problem = new
        {
            statusCode = httpContext.Response.StatusCode,
            message = "An unexpected error occurred. Please try again later."
        };

        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
