using Approva.Domain.Common;

namespace Approva.Domain.Entities;

public class Tenant : Entity
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private Tenant()
    {
    }

    public static Tenant Create(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del tenant es obligatorio.");
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("El slug del tenant es obligatorio.");

        return new Tenant
        {
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
