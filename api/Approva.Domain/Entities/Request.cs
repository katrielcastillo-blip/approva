using System.Text.Json;
using Approva.Domain.Common;
using Approva.Domain.Enums;

namespace Approva.Domain.Entities;

public class Request : Entity
{
    public Guid TenantId { get; private set; }
    public Guid WorkflowDefinitionId { get; private set; }
    public Guid RequesterId { get; private set; }
    public string Title { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;

    /// <summary>Arbitrary extra fields (department, cost center, etc.) as a JSON object.
    /// Mapped to a jsonb column in Postgres.</summary>
    public string PayloadJson { get; private set; } = "{}";

    public Guid? CurrentStepId { get; private set; }
    public RequestStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private Request()
    {
    }

    public static Request Create(
        Guid tenantId,
        Guid workflowDefinitionId,
        Guid requesterId,
        string title,
        decimal amount,
        string currency,
        string payloadJson = "{}")
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("El título de la solicitud es obligatorio.");
        if (amount < 0)
            throw new DomainException("El monto no puede ser negativo.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("La moneda es obligatoria.");

        ValidatePayload(payloadJson);

        return new Request
        {
            TenantId = tenantId,
            WorkflowDefinitionId = workflowDefinitionId,
            RequesterId = requesterId,
            Title = title.Trim(),
            Amount = amount,
            Currency = currency.Trim().ToUpperInvariant(),
            PayloadJson = payloadJson,
            Status = RequestStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Draft -> Pending (a step applies) or Draft -> Approved (no step applies, auto-approved).</summary>
    public void Submit(Guid? firstStepId)
    {
        if (Status != RequestStatus.Draft)
            throw new DomainException($"Solo se puede enviar una solicitud en estado Draft (actual: {Status}).");

        CurrentStepId = firstStepId;
        Status = firstStepId is null ? RequestStatus.Approved : RequestStatus.Pending;
        if (Status == RequestStatus.Approved)
            CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Called after the current ApprovalTask is approved and the engine determines
    /// the next step. Pending -> Pending (next step) or Pending -> Approved (no steps left).</summary>
    public void AdvanceTo(Guid? nextStepId)
    {
        if (Status != RequestStatus.Pending)
            throw new DomainException($"Solo se puede avanzar una solicitud en estado Pending (actual: {Status}).");

        CurrentStepId = nextStepId;
        if (nextStepId is null)
        {
            Status = RequestStatus.Approved;
            CompletedAt = DateTimeOffset.UtcNow;
        }
    }

    public void Reject()
    {
        if (Status != RequestStatus.Pending)
            throw new DomainException($"Solo se puede rechazar una solicitud en estado Pending (actual: {Status}).");

        Status = RequestStatus.Rejected;
        CurrentStepId = null;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status is not (RequestStatus.Draft or RequestStatus.Pending))
            throw new DomainException($"No se puede cancelar una solicitud en estado {Status}.");

        Status = RequestStatus.Cancelled;
        CurrentStepId = null;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Resolves a field for condition evaluation: well-known Request properties
    /// first (Amount, Currency, RequesterId), then the JSON payload.</summary>
    public string? GetFieldValue(string field)
    {
        switch (field)
        {
            case "Amount":
                return Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            case "Currency":
                return Currency;
            case "RequesterId":
                return RequesterId.ToString();
        }

        using var doc = JsonDocument.Parse(PayloadJson);
        if (!doc.RootElement.TryGetProperty(field, out var value))
            return null;

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static void ValidatePayload(string payloadJson)
    {
        try
        {
            using var _ = JsonDocument.Parse(payloadJson);
        }
        catch (JsonException ex)
        {
            throw new DomainException($"El payload no es un JSON válido: {ex.Message}");
        }
    }
}
