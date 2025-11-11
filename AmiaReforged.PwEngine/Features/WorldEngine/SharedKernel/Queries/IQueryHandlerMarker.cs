namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

/// <summary>
/// Marker interface for query handler auto-discovery via Anvil DI.
/// All query handlers are automatically injected via this marker.
/// </summary>
public interface IQueryHandlerMarker
{
    // Marker interface - no members required
    // Used by Anvil DI to collect all query handler implementations
}

