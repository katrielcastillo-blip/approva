using Approva.Domain.Enums;

namespace Approva.Application.Common.Interfaces;

/// <summary>Resolves the authenticated caller from the current HTTP request's JWT claims.
/// Implemented in the Api layer (needs IHttpContextAccessor), consumed by Application
/// handlers so they never touch ASP.NET types directly.</summary>
public interface ICurrentUserService
{
    Guid TenantId { get; }
    Guid UserId { get; }
    UserRole Role { get; }
}
