using System.Security.Claims;
using Approva.Application.Common.Interfaces;
using Approva.Domain.Enums;
using Approva.Infrastructure.Auth;

namespace Approva.Api.Services;

/// <summary>Reads the authenticated caller from JWT claims on the current HTTP request.
/// Anonymous requests (login, tenant signup, health) get an empty/default identity —
/// never throws — so the DbContext's global tenant query filter degrades safely instead
/// of blowing up on public endpoints.
///
/// Reads HttpContext.User lazily on every property access rather than snapshotting it in
/// the constructor: this service is scoped (one instance per request), and something
/// resolving it before ASP.NET Core finishes assigning the authenticated principal to
/// HttpContext.User (e.g. a JwtBearerEvents.OnTokenValidated handler pulling in the
/// DbContext, which depends on this) would otherwise permanently cache an empty/anonymous
/// identity for the rest of that request — silently resolving every TenantId to
/// Guid.Empty downstream, well past the point where the user is actually authenticated.</summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid TenantId => TryGetGuidClaim(ApprovaClaimTypes.TenantId) ?? Guid.Empty;

    public Guid UserId => TryGetGuidClaim(ClaimTypes.NameIdentifier) ?? Guid.Empty;

    public UserRole Role
    {
        get
        {
            var value = User?.FindFirstValue(ClaimTypes.Role);
            return value is not null && Enum.TryParse<UserRole>(value, out var role) ? role : UserRole.Requester;
        }
    }

    private Guid? TryGetGuidClaim(string claimType)
    {
        var value = User?.FindFirstValue(claimType);
        return Guid.TryParse(value, out var guid) ? guid : null;
    }
}
