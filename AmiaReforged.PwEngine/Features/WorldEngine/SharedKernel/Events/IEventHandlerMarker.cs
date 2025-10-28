namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

/// <summary>
/// Marker interface for event handlers to enable collection injection.
/// All event handlers should implement this interface along with IEventHandler&lt;TEvent&gt;.
/// This allows the AnvilEventBusService to discover and inject all event handlers.
/// </summary>
public interface IEventHandlerMarker
{
}

