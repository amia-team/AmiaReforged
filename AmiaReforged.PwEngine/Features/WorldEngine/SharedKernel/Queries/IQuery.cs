namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

/// <summary>
/// Marker interface for queries that return typed results.
/// Queries are read-only operations that retrieve data without side effects.
/// </summary>
/// <typeparam name="TResult">The type of result this query returns.</typeparam>
public interface IQuery<TResult>
{
    // Marker interface - no members required
    // Implementation classes define their own properties for query parameters
}

