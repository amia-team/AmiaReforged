namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

/// <summary>
/// Marker interface for command handler auto-discovery via Anvil DI.
/// All command handlers are automatically injected via this marker.
/// </summary>
public interface ICommandHandlerMarker
{
    // Marker interface - no members required
    // Used by Anvil DI to collect all command handler implementations
}

