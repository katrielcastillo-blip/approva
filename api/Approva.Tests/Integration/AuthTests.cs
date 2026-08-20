using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Approva.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Approva.Tests.Integration;

/// <summary>Regression test for a real bug found manually: a JWT's signature stays valid
/// even after the user it names is gone (deleted, or — the way this actually surfaced —
/// a local dev database reset that regenerates every row's id). Without a check, every
/// protected endpoint kept "succeeding" against a resolved TenantId that matches nothing,
/// so every list silently came back empty instead of the app asking for a fresh login.</summary>
public class AuthTests : IClassFixture<ApprovaWebApplicationFactory>
{
    private readonly ApprovaWebApplicationFactory _factory;

    public AuthTests(ApprovaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private record AuthResult(string Token, Guid UserId, Guid TenantId, string Email, string Name, string Role);

    [Fact]
    public async Task TokenForADeletedUser_IsRejectedWithUnauthorized_NotSilentlyEmptyData()
    {
        var slug = $"auth-{Guid.NewGuid():N}"[..12];
        var client = _factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/auth/register-tenant", new
        {
            tenantName = "Auth Co",
            tenantSlug = slug,
            adminName = "Admin",
            adminEmail = $"admin@{slug}.test",
            adminPassword = "TestPass123!"
        });
        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResult>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        // The token is still perfectly valid here — same user, same tenant.
        var beforeDelete = await client.GetAsync("/requests");
        Assert.Equal(HttpStatusCode.OK, beforeDelete.StatusCode);

        // Simulate the user disappearing from under the token (deleted account, or — the
        // scenario that actually triggered this bug — a full database reset elsewhere).
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApprovaDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM users WHERE \"Id\" = {auth.UserId}");
        }

        var afterDelete = await client.GetAsync("/requests");

        Assert.Equal(HttpStatusCode.Unauthorized, afterDelete.StatusCode);
    }
}
