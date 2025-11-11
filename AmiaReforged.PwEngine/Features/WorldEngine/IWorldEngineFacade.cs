using AmiaReforged.PwEngine.Features.WorldEngine.Core.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy;

namespace AmiaReforged.PwEngine.Features.WorldEngine;

/// <summary>
/// Facade providing unified access to all WorldEngine subsystems.
/// This simplifies interaction by grouping related functionality and reducing
/// the number of dependencies that need to be injected.
/// </summary>
public interface IWorldEngineFacade
{
    // === Cross-Cutting Gateways ===
    // These are fundamental concerns that span multiple subsystems

    /// <summary>
    /// Access to persona identity and relationship operations.
    /// Personas are a cross-cutting concern used throughout the world engine
    /// for representing any actor (players, characters, organizations, etc.)
    /// </summary>
    IPersonaGateway Personas { get; }

    // === Subsystems ===

    /// <summary>
    /// Access to economy-related operations (banking, transactions, shops, storage)
    /// </summary>
    IEconomySubsystem Economy { get; }

    /// <summary>
    /// Access to organization-related operations (creation, membership, diplomacy)
    /// </summary>
    IOrganizationSubsystem Organizations { get; }

    /// <summary>
    /// Access to character-related operations (registration, stats, reputation)
    /// </summary>
    ICharacterSubsystem Characters { get; }

    /// <summary>
    /// Access to industry-related operations (crafting, recipes, learning)
    /// </summary>
    IIndustrySubsystem Industries { get; }

    /// <summary>
    /// Access to harvesting-related operations (resource nodes, gathering)
    /// </summary>
    IHarvestingSubsystem Harvesting { get; }

    /// <summary>
    /// Access to region-related operations (area management, regional effects)
    /// </summary>
    IRegionSubsystem Regions { get; }

    /// <summary>
    /// Access to trait-related operations (character traits, trait effects)
    /// </summary>
    ITraitSubsystem Traits { get; }

    /// <summary>
    /// Access to item-related operations (item definitions, properties)
    /// </summary>
    IItemSubsystem Items { get; }

    /// <summary>
    /// Access to codex-related operations (knowledge management, lore)
    /// </summary>
    ICodexSubsystem Codex { get; }

    // === Centralized Command/Query Dispatch ===

    /// <summary>
    /// Executes a command through the centralized dispatcher.
    /// Automatically routes to the correct handler based on command type.
    /// </summary>
    /// <example>
    /// <code>
    /// var command = new DepositGoldCommand(...);
    /// var result = await worldEngine.ExecuteAsync(command);
    /// </code>
    /// </example>
    Task<CommandResult> ExecuteAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    /// <summary>
    /// Executes a query through the centralized dispatcher.
    /// Automatically routes to the correct handler based on query type.
    /// </summary>
    /// <example>
    /// <code>
    /// var query = new GetStoredItemsQuery(...);
    /// var items = await worldEngine.QueryAsync&lt;GetStoredItemsQuery, List&lt;StoredItemDto&gt;&gt;(query);
    /// </code>
    /// </example>
    Task<TResult> QueryAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>;

    /// <summary>
    /// Executes multiple commands in a batch operation.
    /// </summary>
    Task<BatchCommandResult> ExecuteBatchAsync<TCommand>(
        IEnumerable<TCommand> commands,
        BatchExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand;
}

