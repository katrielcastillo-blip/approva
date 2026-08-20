using System.Security.Claims;
using Approva.Application.Common.Interfaces;
using Approva.Domain.Enums;
using Approva.Infrastructure.Auth;

namespace Approva.Api.Services;

/// <summary>Reads the authenticated caller from JWT claims on the current HTTP request.
/// Anonymous requests (login, tenant signup, health) get an empty/default identity —
/// never throws — so the DbContext's global tenant query filter degrades safely instead
/// of blowing up on public endpoints.</summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly ClaimsPrincipal? _user;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _user = httpContextAccessor.HttpContext?.User;
    }

    public Guid TenantId => TryGetGuidClaim(ApprovaClaimTypes.TenantId) ?? Guid.Empty;

    public Guid UserId => TryGetGuidClaim(ClaimTypes.NameIdentifier) ?? Guid.Empty;

    public UserRole Role
    {
        get
        {
            var value = _user?.FindFirstValue(ClaimTypes.Role);
            return value is not null && Enum.TryParse<UserRole>(value, out var role) ? role : UserRole.Requester;
        }
    }

    private Guid? TryGetGuidClaim(string claimType)
    {
        var value = _user?.FindFirstValue(claimType);
        return Guid.TryParse(value, out var guid) ? guid : null;
    }
}
