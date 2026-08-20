using Approva.Domain.Common;
using Approva.Domain.Enums;

namespace Approva.Domain.Entities;

public class User : Entity
{
    public Guid TenantId { get; private set; }
    public string Email { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public UserRole Role { get; private set; }

    /// <summary>Organizational title used for workflow routing (e.g. "CFO", "HR Manager").
    /// Distinct from the RBAC <see cref="Role"/>.</summary>
    public string? ApproverRole { get; private set; }

    public Guid? ManagerId { get; private set; }
    public bool IsOutOfOffice { get; private set; }
    public Guid? DelegateUserId { get; private set; }

    /// <summary>Hashed password. Null for users provisioned without local auth.</summary>
    public string? PasswordHash { get; private set; }

    private User()
    {
    }

    public static User Create(
        Guid tenantId,
        string email,
        string name,
        UserRole role,
        string passwordHash,
        string? approverRole = null,
        Guid? managerId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("El email es obligatorio.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("El password hash es obligatorio.");

        return new User
        {
            TenantId = tenantId,
            Email = email.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            Role = role,
            ApproverRole = string.IsNullOrWhiteSpace(approverRole) ? null : approverRole.Trim(),
            ManagerId = managerId,
            PasswordHash = passwordHash,
            IsOutOfOffice = false
        };
    }

    public void SetManager(Guid? managerId)
    {
        if (managerId == Id)
            throw new DomainException("Un usuario no puede ser su propio manager.");

        ManagerId = managerId;
    }

    public void SetOutOfOffice(bool isOutOfOffice, Guid? delegateUserId)
    {
        if (isOutOfOffice && delegateUserId is null)
            throw new DomainException("Fuera de oficina requiere un usuario delegado.");
        if (delegateUserId == Id)
            throw new DomainException("Un usuario no puede delegarse a sí mismo.");

        IsOutOfOffice = isOutOfOffice;
        DelegateUserId = isOutOfOffice ? delegateUserId : null;
    }

    /// <summary>Resolves who should actually receive tasks assigned to this user
    /// right now: the delegate if out-of-office, otherwise the user itself.</summary>
    public Guid EffectiveAssigneeId => IsOutOfOffice && DelegateUserId.HasValue ? DelegateUserId.Value : Id;
}
