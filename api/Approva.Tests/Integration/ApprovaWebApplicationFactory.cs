using Approva.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Approva.Tests.Integration;

/// <summary>Boots the real Approva.Api host against a real, disposable Postgres
/// container (Testcontainers) — no mocks. Migrations run once per test class; seeding
/// is disabled so each test builds exactly the data it needs through the HTTP API,
/// which is what actually exercises tenant isolation end to end.</summary>
public class ApprovaWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("approva_test")
        .WithUsername("approva")
        .WithPassword("approva_test")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:Secret", "integration-test-secret-key-at-least-32-chars-long");
        builder.UseSetting("Seed:OnStartup", "false");
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApprovaDbContext>();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
