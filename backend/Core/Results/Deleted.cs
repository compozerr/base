using Microsoft.AspNetCore.Http;

namespace Core.Results;

public class Deleted : IResult, IStatusCodeHttpResult
{
    public static int StatusCode => StatusCodes.Status410Gone;
    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        httpContext.Response.StatusCode = StatusCodes.Status410Gone;
        return Task.CompletedTask;
    }
}
