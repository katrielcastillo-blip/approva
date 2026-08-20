using Approva.Application.Auth.Dtos;
using MediatR;

namespace Approva.Application.Auth.Commands.RegisterTenant;

/// <summary>Self-service signup: creates a new Tenant plus its first user (an Admin),
/// and logs them in immediately.</summary>
public record RegisterTenantCommand(
    string TenantName,
    string TenantSlug,
    string AdminName,
    string AdminEmail,
    string AdminPassword) : IRequest<AuthResultDto>;
