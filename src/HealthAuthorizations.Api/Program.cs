using HealthAuthorizations.Api;
using HealthAuthorizations.Api.Middleware;
using HealthAuthorizations.Application;
using HealthAuthorizations.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, logger) => logger
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services
        .AddApplication(builder.Configuration)
        .AddInfrastructure(builder.Configuration)
        .AddPresentation(builder.Configuration);

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("Health Authorizations API"));

    app.MapControllers();

    // live: el proceso responde. ready: además sus dependencias (PostgreSQL) están disponibles.
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });

    if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
    {
        await app.ApplyMigrationsAsync();
    }

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "La aplicación terminó de forma inesperada");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Expuesto para WebApplicationFactory en las pruebas de integración.
public partial class Program;
