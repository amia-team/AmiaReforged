using System.Reflection.PortableExecutable;
using Anvil.API;
using NLog;
using NLog.Fluent;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.GeneralFeats;

public class MonkeyGrip(NwCreature creature)
{
    private const int MonkeyGripVisualEffect = 2527;
    private const string LocalIntBaseSize = "base_size";
    private const string PcKeyTag = "ds_pckey";

    public void ChangeSize()
    {
        int baseSize = GetBaseSize();

        bool shouldApplyMg = creature.Size == (CreatureSize)baseSize;

        CreatureSize targetSize = shouldApplyMg ? (CreatureSize)Math.Clamp(baseSize + 1, 0, 5) : (CreatureSize)baseSize;

        creature.Size = targetSize;

        if (shouldApplyMg)
        {
            ApplyMgPenalty();
        }
        else
        {
            UnequipOffhand();
            RemoveMgPenalty();
            ApplyVisualEffect();
        }
    }

    private int GetBaseSize()
    {
        NwItem? pcKey = creature.FindItemWithTag(PcKeyTag);

        if (pcKey is null) return 0;

        int baseSize = NWScript.GetLocalInt(pcKey, LocalIntBaseSize);

        // Store the base size to the character's PC key if it has not yet been set
        if (baseSize == NWScript.CREATURE_SIZE_INVALID)
        {
            baseSize = (int)creature.Size;
            NWScript.SetLocalInt(pcKey, LocalIntBaseSize, baseSize);

            if (creature.IsPlayerControlled(out NwPlayer? _))
            {
                NWScript.ExportSingleCharacter(creature);
            }
        }

        return baseSize;
    }

    private void UnequipOffhand()
    {
        NwItem? offhand = creature.GetItemInSlot(InventorySlot.LeftHand);
        if (offhand is not null)
        {
            creature.ActionUnequipItem(offhand);
        }
    }

    public bool IsMonkeyGripped()
    {
        NwItem? pcKey = creature.FindItemWithTag(PcKeyTag);

        if (pcKey is null) return false;

        int baseSize = NWScript.GetLocalInt(pcKey, LocalIntBaseSize);

        // Set the int if it hasn't been set...
        if (baseSize == 0)
        {
            baseSize = (int)creature.Size;
            NWScript.SetLocalInt(pcKey, LocalIntBaseSize, baseSize);
        }

        return creature.Size != (CreatureSize)baseSize;
    }

    public void ApplyMgPenalty()
    {
        Effect? existing = creature.ActiveEffects.FirstOrDefault(effect => effect.Tag == "mg_penalty");
        if (existing is not null)
        {
            creature.RemoveEffect(existing);
        }

        Effect mgPenalty = Effect.AttackDecrease(2);
        mgPenalty.SubType = EffectSubType.Supernatural;
        mgPenalty.Tag = "mg_penalty";

        ApplyVisualEffect();

        creature.ApplyEffect(EffectDuration.Permanent, mgPenalty);
        PlayerPlugin.UpdateCharacterSheet(creature);
    }

    private void RemoveMgPenalty()
    {
        Effect? mgPenalty = creature.ActiveEffects.FirstOrDefault(e => e.Tag == "mg_penalty");

        if (mgPenalty is null) return;
        
        creature.RemoveEffect(mgPenalty);
        PlayerPlugin.UpdateCharacterSheet(creature);
    }

    private void ApplyVisualEffect()
    {
        Effect? mgEffect = NWScript.EffectVisualEffect(MonkeyGripVisualEffect);
        
        if (mgEffect is null)
        {
            LogManager.GetCurrentClassLogger().Error("MonkeyGrip effect is null");
            return;
        }

        creature.ApplyEffect(EffectDuration.Instant, mgEffect);
    }
}