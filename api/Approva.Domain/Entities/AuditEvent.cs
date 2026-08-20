using Approva.Domain.Common;
using Approva.Domain.Enums;

namespace Approva.Domain.Entities;

/// <summary>Append-only audit trail. No update or delete operations are exposed anywhere
/// in the domain, application, or infrastructure layers — not even in the repository.</summary>
public class AuditEvent : Entity
{
    public Guid TenantId { get; private set; }
    public Guid RequestId { get; private set; }
    public Guid ActorId { get; private set; }
    public AuditEventType EventType { get; private set; }
    public string PayloadJson { get; private set; } = "{}";
    public DateTimeOffset OccurredAt { get; private set; }

    private AuditEvent()
    {
    }

    public static AuditEvent Create(Guid tenantId, Guid requestId, Guid actorId, AuditEventType eventType, string payloadJson = "{}")
    {
        return new AuditEvent
        {
            TenantId = tenantId,
            RequestId = requestId,
            ActorId = actorId,
            EventType = eventType,
            PayloadJson = payloadJson,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}
