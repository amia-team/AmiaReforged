using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;

namespace AmiaReforged.PwEngine.Features.WorldEngine;

/// <summary>
/// Facade providing unified access to all WorldEngine subsystems.
/// This simplifies interaction by grouping related functionality and reducing
/// the number of dependencies that need to be injected.
/// </summary>
public interface IWorldEngineFacade
{
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
}

