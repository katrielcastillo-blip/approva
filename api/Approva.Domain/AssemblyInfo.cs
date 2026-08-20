using System.Runtime.CompilerServices;

// Lets Infrastructure backdate timestamps on seeded demo data (see ApprovalTask.BackdateForSeed
// and AuditEvent.BackdateForSeed) without exposing that capability on the public domain API.
[assembly: InternalsVisibleTo("Approva.Infrastructure")]
