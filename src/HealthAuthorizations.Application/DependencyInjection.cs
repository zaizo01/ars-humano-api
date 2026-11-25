using FluentValidation;
using HealthAuthorizations.Application.Behaviors;
using HealthAuthorizations.Application.Common;
using HealthAuthorizations.Domain.Authorizations;
using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthAuthorizations.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = configuration["MediatR:LicenseKey"];
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        TypeAdapterConfig.GlobalSettings.Scan(assembly);

        services.AddOptions<CoverageOptions>()
            .Bind(configuration.GetSection(CoverageOptions.SectionName))
            .Validate(o => o.CoveragePercentage is >= 0m and <= 100m, "Coverage:CoveragePercentage debe estar entre 0 y 100.")
            .ValidateOnStart();

        services.AddSingleton<IAmountDistributionPolicy>(sp =>
            new PercentageDistributionPolicy(sp.GetRequiredService<IOptions<CoverageOptions>>().Value.CoveragePercentage));

        return services;
    }
}
