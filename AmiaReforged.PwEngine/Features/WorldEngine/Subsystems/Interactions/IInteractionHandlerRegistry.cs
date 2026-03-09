namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;

/// <summary>
/// Registry that maps interaction tags to their <see cref="IInteractionHandler"/> implementations.
/// </summary>
public interface IInteractionHandlerRegistry
{
    /// <summary>
    /// Returns the handler registered for <paramref name="interactionTag"/>,
    /// or <c>null</c> if no handler exists for that tag.
    /// </summary>
    IInteractionHandler? GetHandler(string interactionTag);

    /// <summary>
    /// Returns all registered handlers.
    /// </summary>
    IReadOnlyCollection<IInteractionHandler> GetAll();
}
