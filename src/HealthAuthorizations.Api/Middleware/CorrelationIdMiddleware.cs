using Serilog.Context;

namespace HealthAuthorizations.Api.Middleware;

/// <summary>
/// Propaga (o genera) el header X-Correlation-Id y lo agrega a todos los logs
/// del request, para poder rastrear un incidente de punta a punta.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault() is { Length: > 0 and <= 64 } incoming
            ? incoming
            : Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(ItemKey, correlationId))
        {
            await next(context);
        }
    }
}
