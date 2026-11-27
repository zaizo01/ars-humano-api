using HealthAuthorizations.Infrastructure.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace HealthAuthorizations.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public CapturingEventPublisher Events { get; } = new();

    public Task StartDatabaseAsync() => _db.StartAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Database", _db.GetConnectionString());
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Outbox:PollingInterval", "00:00:00.200");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEventPublisher>();
            services.AddSingleton<IEventPublisher>(Events);
        });
    }
}

// Una sola API y base para todo el ensamblado.
[SetUpFixture]
public sealed class IntegrationTestSetup
{
    public static ApiFactory Factory { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        Factory = new ApiFactory();
        await Factory.StartDatabaseAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() => await Factory.DisposeAsync();
}
