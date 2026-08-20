namespace Approva.Application.Common.Models;

/// <summary>Backs Idempotency-Key handling on unsafe endpoints (e.g. POST decisions):
/// the first request for a key runs and stores its response; replays of the same key
/// return the stored response without re-executing the command.</summary>
public class IdempotencyRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Key { get; set; } = null!;
    public string RequestPath { get; set; } = null!;
    public int ResponseStatusCode { get; set; }
    public string ResponseBodyJson { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}
