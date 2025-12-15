namespace AmiaReforged.PwEngine.Features.AI.Core.Models;

/// <summary>
/// Classification of a creature for AI and reward purposes.
/// Ported from GetCreatureType() in inc_ds_ondeath.nss.
/// </summary>
public enum CreatureClassification
{
    /// <summary>
    /// Invalid object or error.
    /// </summary>
    Invalid = -1,

    /// <summary>
    /// Not a creature (placeable, item, etc.).
    /// </summary>
    NotCreature = 0,

    /// <summary>
    /// Standard NPC (non-associated creature).
    /// </summary>
    Npc = 1,

    /// <summary>
    /// Dominated NPC.
    /// </summary>
    DominatedNpc = 2,

    /// <summary>
    /// Summoned creature.
    /// </summary>
    Summon = 3,

    /// <summary>
    /// Animal companion.
    /// </summary>
    Companion = 4,

    /// <summary>
    /// Henchman.
    /// </summary>
    Henchman = 5,

    /// <summary>
    /// Familiar.
    /// </summary>
    Familiar = 6,

    /// <summary>
    /// Possessed familiar (PC controlling familiar).
    /// </summary>
    PossessedFamiliar = 7,

    /// <summary>
    /// Player character.
    /// </summary>
    PlayerCharacter = 8,

    /// <summary>
    /// DM-possessed creature.
    /// </summary>
    DmPossessed = 9,

    /// <summary>
    /// DM avatar.
    /// </summary>
    DmAvatar = 10
}

/// <summary>
/// Extension methods for CreatureClassification.
/// </summary>
public static class CreatureClassificationExtensions
{
    /// <summary>
    /// Returns true if this is a player-controlled entity.
    /// </summary>
    public static bool IsPlayerControlled(this CreatureClassification classification) =>
        classification is CreatureClassification.PlayerCharacter
            or CreatureClassification.PossessedFamiliar
            or CreatureClassification.DmPossessed
            or CreatureClassification.DmAvatar;

    /// <summary>
    /// Returns true if this is an associate (summon, familiar, henchman, etc.).
    /// </summary>
    public static bool IsAssociate(this CreatureClassification classification) =>
        classification is CreatureClassification.DominatedNpc
            or CreatureClassification.Summon
            or CreatureClassification.Companion
            or CreatureClassification.Henchman
            or CreatureClassification.Familiar;

    /// <summary>
    /// Returns true if this classification should receive XP rewards.
    /// </summary>
    public static bool CanReceiveXp(this CreatureClassification classification) =>
        classification >= CreatureClassification.DominatedNpc;
}
