using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HealthAuthorizations.Application.Behaviors;

/// <summary>Registra duración de cada request y advierte cuando es lento.</summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 500;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var start = Stopwatch.GetTimestamp();

        var response = await next(cancellationToken);

        var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        if (elapsedMs > SlowRequestThresholdMs)
        {
            logger.LogWarning("Request lento {RequestName} completado en {ElapsedMs:0} ms", requestName, elapsedMs);
        }
        else
        {
            logger.LogInformation("Request {RequestName} completado en {ElapsedMs:0} ms", requestName, elapsedMs);
        }

        return response;
    }
}
