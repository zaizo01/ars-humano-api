using System.Text;
using HealthAuthorizations.Application.Abstractions;
using HealthAuthorizations.Infrastructure.Auth;
using HealthAuthorizations.Infrastructure.Messaging;
using HealthAuthorizations.Infrastructure.Outbox;
using HealthAuthorizations.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace HealthAuthorizations.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Database";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        AddPersistence(services, configuration);
        AddSecurity(services, configuration);
        AddMessaging(services, configuration);

        return services;
    }

    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        var rabbitMq = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ?? new RabbitMqOptions();
        if (rabbitMq.Enabled)
        {
            services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        }
        else
        {
            services.AddSingleton<IEventPublisher, LoggingEventPublisher>();
        }

        services.AddHostedService<OutboxProcessor>();
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Falta la cadena de conexión '{ConnectionStringName}'.");

        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3))
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IAuthorizationNumberGenerator, AuthorizationNumberGenerator>();
    }

    private static void AddSecurity(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => o.SigningKey.Length >= 32, "Jwt:SigningKey debe tener al menos 32 caracteres.")
            .ValidateOnStart();
        services.Configure<List<DemoUserOptions>>(configuration.GetSection(DemoUserOptions.SectionName));
        services.AddSingleton<JwtTokenService>();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Falta la sección de configuración 'Jwt'.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = System.Security.Claims.ClaimTypes.Name,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.AuthorizationsRead, p => p.RequireRole(Roles.Operator, Roles.Auditor))
            .AddPolicy(Policies.AuthorizationsWrite, p => p.RequireRole(Roles.Operator));
    }

    /// <summary>Aplica migraciones pendientes. Pensado para entornos locales / docker compose.</summary>
    public static async Task ApplyMigrationsAsync(this IHost host)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DependencyInjection));

        logger.LogInformation("Aplicando migraciones de base de datos...");
        await db.Database.MigrateAsync();
    }
}
