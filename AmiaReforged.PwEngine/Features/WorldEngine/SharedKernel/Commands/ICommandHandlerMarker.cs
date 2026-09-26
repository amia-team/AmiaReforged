namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

/// <summary>
/// Non-generic marker for command-handler discovery.
/// ICommandHandler&lt;TCommand&gt; implementations inherit this marker so the
/// CQRS registry can identify handler implementation types without requiring
/// every handler to be separately registered under the marker service type.
/// </summary>
public interface ICommandHandlerMarker
{
}
