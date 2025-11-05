namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

/// <summary>
/// Represents the type of persona/actor in the world system.
/// </summary>
public enum PersonaType
{
    /// <summary>
    /// A player account identified by CD key.
    /// </summary>
    Player,

    /// <summary>
    /// A player character.
    /// </summary>
    Character,

    /// <summary>
    /// A player-created or DM-created organization (guild, faction, company).
    /// </summary>
    Organization,

    /// <summary>
    /// A banking/treasury institution tied to a settlement.
    /// </summary>
    Coinhouse,

    /// <summary>
    /// A storage facility for goods and materials.
    /// </summary>
    Warehouse,

    /// <summary>
    /// A settlement's governing body.
    /// </summary>
    Government,

    /// <summary>
    /// An automated system process (tax collection, decay, market rebalancing).
    /// </summary>
    SystemProcess
}

