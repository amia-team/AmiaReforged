using AmiaReforged.PwEngine.Features.WorldEngine.Core.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine;

/// <summary>
/// Concrete implementation of the WorldEngine facade.
/// Provides unified access to all WorldEngine subsystems through dependency injection.
/// </summary>
[ServiceBinding(typeof(IWorldEngineFacade))]
public sealed class WorldEngineFacade : IWorldEngineFacade
{
    public WorldEngineFacade(
        IPersonaFacade personas,
        IEconomySubsystem economy,
        IOrganizationSubsystem organizations,
        ICharacterSubsystem characters,
        IIndustrySubsystem industries,
        IHarvestingSubsystem harvesting,
        IRegionSubsystem regions,
        ITraitSubsystem traits,
        IItemSubsystem items,
        ICodexSubsystem codex)
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
    }

    public IPersonaFacade Personas { get; }
    public IEconomySubsystem Economy { get; }
    public IOrganizationSubsystem Organizations { get; }
    public ICharacterSubsystem Characters { get; }
    public IIndustrySubsystem Industries { get; }
    public IHarvestingSubsystem Harvesting { get; }
    public IRegionSubsystem Regions { get; }
    public ITraitSubsystem Traits { get; }
    public IItemSubsystem Items { get; }
    public ICodexSubsystem Codex { get; }
}

