using Anvil.API;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Core.Interfaces;
using AmiaReforged.PwEngine.Features.AI.Core.Models;

namespace AmiaReforged.PwEngine.Features.AI.Core.Services;

/// <summary>
/// Classifies creatures by type for AI and reward purposes.
/// Ported from GetCreatureType() in inc_ds_ondeath.nss.
/// </summary>
[ServiceBinding(typeof(ICreatureClassifier))]
public class CreatureClassifier : ICreatureClassifier
{
    /// <inheritdoc />
    public CreatureClassification Classify(NwGameObject gameObject)
    {
        if (gameObject == null || !gameObject.IsValid)
        {
            return CreatureClassification.Invalid;
        }

        if (gameObject is not NwCreature creature)
        {
            return CreatureClassification.NotCreature;
        }

        // Check DM states first
        if (creature.IsDMPossessed)
        {
            return CreatureClassification.DmPossessed;
        }

        if (creature.IsDMAvatar || creature.IsDMPossessed)
        {
            return CreatureClassification.DmAvatar;
        }

        // Check possessed familiar
        if (creature.IsPossessedFamiliar)
        {
            return CreatureClassification.PossessedFamiliar;
        }

        // Check if PC
        if (creature.IsPlayerControlled)
        {
            return CreatureClassification.PlayerCharacter;
        }

        // Check associate types
        return creature.AssociateType switch
        {
            AssociateType.Dominated => CreatureClassification.DominatedNpc,
            AssociateType.Summoned => CreatureClassification.Summon,
            AssociateType.AnimalCompanion => CreatureClassification.Companion,
            AssociateType.Henchman => CreatureClassification.Henchman,
            AssociateType.Familiar => CreatureClassification.Familiar,
            _ => CreatureClassification.Npc
        };
    }
}
