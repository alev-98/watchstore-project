namespace WatchStore.Api.Common.ExceptionHandlers;

/// <summary>
/// Gestore globale di eccezioni
/// </summary>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ActivityTraceId? traceId = Activity.Current?.TraceId;

        logger.LogError(exception, "Unhandled Exception. TraceId: {TraceId}", traceId);

        ProblemDetails problemDetails = new()
        {
            Title = "An error has occurred",
            Status = StatusCodes.Status500InternalServerError,
            Extensions = new Dictionary<string, object?>()
            {
                { "traceId", traceId.ToString() }
            },
        };

        await Results.Problem(problemDetails).ExecuteAsync(httpContext);

        return true;
    }
}
