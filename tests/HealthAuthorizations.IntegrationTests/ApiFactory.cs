using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace HealthAuthorizations.IntegrationTests;

/// <summary>Levanta la API contra un PostgreSQL real en contenedor (requiere Docker).</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:17-alpine").Build();

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
    }
}

/// <summary>Una sola API + base de datos para todas las pruebas del ensamblado.</summary>
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
