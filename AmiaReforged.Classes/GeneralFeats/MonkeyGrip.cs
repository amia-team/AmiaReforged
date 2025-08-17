using Anvil.API;
using NLog;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.GeneralFeats;

public class MonkeyGrip(NwCreature creature)
{
    private const int MonkeyGripVisualEffect = 2527;
    private const string LocalIntBaseSize = "base_size";
    private const string PcKeyTag = "ds_pckey";

    public void ApplyMonkeyGrip()
    {
        if (IsMonkeyGripped())
        {
            bool didUnequip = UnequipOffhand();
            if (didUnequip == false) return;
        }

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
        if (baseSize != NWScript.CREATURE_SIZE_INVALID) return baseSize;

        baseSize = (int)creature.Size;
        NWScript.SetLocalInt(pcKey, LocalIntBaseSize, baseSize);

        if (creature.IsPlayerControlled(out NwPlayer? _))
        {
            NWScript.ExportSingleCharacter(creature);
        }

        return baseSize;
    }

    private bool UnequipOffhand()
    {
        NwItem? offhand = creature.GetItemInSlot(InventorySlot.LeftHand);
        if (offhand is null) return true;

        if (!creature.Inventory.CheckFit(offhand))
        {
            if (creature.IsPlayerControlled(out NwPlayer? player))
            {
                player.SendServerMessage("Inventory full! Monkey Grip can't unequip offhand item. Make room in inventory to try again.");
            }
            return false;
        }

        bool wasUnequipped = creature.RunUnequip(offhand);

        if (wasUnequipped == false)
        {
            if (creature.IsPlayerControlled(out NwPlayer? player))
            {
                player.SendServerMessage("Monkey Grip can't unequip offhand item for an unknown reason. Try again later.");
            }
        }

        return wasUnequipped;
    }

    private bool IsMonkeyGripped()
    {
        NwItem? mainHandItem = creature.GetItemInSlot(InventorySlot.RightHand);
        if (mainHandItem is null)
            return false;

        NwItem? offHandItem = creature.GetItemInSlot(InventorySlot.LeftHand);
        if (offHandItem is null)
            return false;

        int weaponSize = (int)mainHandItem.BaseItem.WeaponSize;
        int creatureSize = (int)creature.Size;

        // We know that the creature has logged in while monkey gripped if the creature is wielding an offhand item
        // while they are also wielding a weapon larger than their own size
        return weaponSize > creatureSize;
    }

    private void ApplyMgPenalty()
    {
        Effect? existing = creature.ActiveEffects.FirstOrDefault(effect => effect.Tag == "mg_penalty");
        if (existing is not null)
        {
            creature.RemoveEffect(existing);
        }

        Effect mgPenalty = Effect.LinkEffects(Effect.AttackDecrease(1), Effect.ACIncrease(1),
            Effect.SkillIncrease(NwSkill.FromSkillType(Skill.Hide)!, 4),
            Effect.SkillIncrease(NwSkill.FromSkillType(Skill.MoveSilently)!, 4),
            Effect.SkillIncrease(NwSkill.FromSkillType(Skill.Spot)!, 4),
            Effect.SkillIncrease(NwSkill.FromSkillType(Skill.Listen)!, 4));

        mgPenalty.SubType = EffectSubType.Unyielding;
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
