using AmiaReforged.PwEngine.Features.WorldEngine.Core.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine;

/// <summary>
/// Concrete implementation of the WorldEngine facade.
/// Provides unified access to all WorldEngine subsystems and centralized command/query dispatch.
/// </summary>
[ServiceBinding(typeof(IWorldEngineFacade))]
public sealed class WorldEngineFacade : IWorldEngineFacade
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;

    public WorldEngineFacade(
        IPersonaGateway personas,
        IEconomySubsystem economy,
        IOrganizationSubsystem organizations,
        ICharacterSubsystem characters,
        IIndustrySubsystem industries,
        IHarvestingSubsystem harvesting,
        IRegionSubsystem regions,
        ITraitSubsystem traits,
        IItemSubsystem items,
        ICodexSubsystem codex,
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher)
    {
        Personas = personas;
        Economy = economy;
        Organizations = organizations;
        Characters = characters;
        Industries = industries;
        Harvesting = harvesting;
        Regions = regions;
        Traits = traits;
        Items = items;
        Codex = codex;
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
    }

    public IPersonaGateway Personas { get; }
    public IEconomySubsystem Economy { get; }
    public IOrganizationSubsystem Organizations { get; }
    public ICharacterSubsystem Characters { get; }
    public IIndustrySubsystem Industries { get; }
    public IHarvestingSubsystem Harvesting { get; }
    public IRegionSubsystem Regions { get; }
    public ITraitSubsystem Traits { get; }
    public IItemSubsystem Items { get; }
    public ICodexSubsystem Codex { get; }

    // === Centralized Dispatch Methods ===

    public Task<CommandResult> ExecuteAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
        => _commandDispatcher.DispatchAsync(command, cancellationToken);

    public Task<TResult> QueryAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
        => _queryDispatcher.DispatchAsync<TQuery, TResult>(query, cancellationToken);

    public Task<BatchCommandResult> ExecuteBatchAsync<TCommand>(
        IEnumerable<TCommand> commands,
        BatchExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
        => _commandDispatcher.DispatchBatchAsync(commands, options, cancellationToken);
}

