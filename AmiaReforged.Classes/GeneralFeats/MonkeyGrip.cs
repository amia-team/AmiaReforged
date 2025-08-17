using Anvil.API;
using NLog;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.GeneralFeats;

public class MonkeyGrip(NwCreature creature)
{
    private const int MonkeyGripVisualEffect = 2527;

    public void ApplyMonkeyGrip()
    {
        int baseSize = creature.Appearance.SizeCategory ?? (int)CreatureSize.Medium; // Assume medium if size is invalid

        bool shouldApplyMg = creature.Size == (CreatureSize)baseSize;

        CreatureSize targetSize = shouldApplyMg ? (CreatureSize)Math.Clamp(baseSize + 1, 0, 5) : (CreatureSize)baseSize;

        if (shouldApplyMg)
        {
            ApplyMgPenalty();
        }
        else
        {
            bool didUnequip = UnequipOffhand();
            if (!didUnequip) return;
            RemoveMgPenalty();
            ApplyVisualEffect();
        }

        creature.Size = targetSize;
    }

    private bool UnequipOffhand()
    {
        NwItem? offhand = creature.GetItemInSlot(InventorySlot.LeftHand);
        if (offhand is null) return true;

        NwPlayer? player = creature.ControllingPlayer;

        if (!creature.Inventory.CheckFit(offhand))
        {
            if (player != null)
            {
                player.SendServerMessage("Couldn't deactivate Monkey Grip, because your inventory is full. " +
                                         "Make room in inventory to try again");
            }
            return false;
        }

        bool didUnequip = creature.RunUnequip(offhand);

        if (!didUnequip)
        {
            if (player != null)
            {
                player.SendServerMessage("Couldn't deactivate Monkey Grip, because your offhand item wouldn't unequip" +
                                         " for an unknown error. Try again later.");
            }
        }

        return didUnequip;
    }

    public bool IsMonkeyGripped()
    {
        NwItem? mainHandItem = creature.GetItemInSlot(InventorySlot.RightHand);
        if (mainHandItem is null)
            return false;

        NwItem? offHandItem = creature.GetItemInSlot(InventorySlot.LeftHand);
        if (offHandItem is null)
            return false;

        int weaponSize = (int)mainHandItem.BaseItem.WeaponSize;
        int baseSize = creature.Appearance.SizeCategory ?? (int)CreatureSize.Medium; // Assume medium if invalid

        // We know that the creature is monkey gripped when they're wielding an offhand item
        // while they are also wielding a weapon larger than their own size
        return weaponSize > baseSize;
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
