using System.Text.Json.Serialization;
using HealthAuthorizations.Api.Middleware;
using HealthAuthorizations.Api.OpenApi;
using InfrastructureDI = HealthAuthorizations.Infrastructure.DependencyInjection;

namespace HealthAuthorizations.Api;

internal static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddProblemDetails(options => options.CustomizeProblemDetails = ctx =>
        {
            ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
            if (ctx.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var correlationId))
            {
                ctx.ProblemDetails.Extensions["correlationId"] = correlationId;
            }
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString(InfrastructureDI.ConnectionStringName)!, name: "postgresql", tags: ["ready"]);

        return services;
    }
}
