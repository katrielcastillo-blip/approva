namespace Approva.Application.Common.Exceptions;

/// <summary>Maps to 409 Conflict — used for optimistic-concurrency losses (two approvers
/// deciding the same task at once) and other state conflicts.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
