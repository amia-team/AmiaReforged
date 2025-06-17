using System.Reflection.PortableExecutable;
using Anvil.API;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.GeneralFeats;

public class MonkeyGrip(NwCreature player)
{
    public void ChangeSize()
    {
        NwItem? pcKey = player.FindItemWithTag("ds_pckey");

        if (pcKey is null) return;

        int baseSize = NWScript.GetLocalInt(pcKey, "base_size");

        // Set the int if it hasn't been set...
        if (baseSize == 0)
        {
            baseSize = (int)player.Size;
            NWScript.SetLocalInt(pcKey, "base_size", baseSize);
        }

        if (player.Size == (CreatureSize)baseSize)
        {
            CreatureSize newSize = (CreatureSize)(int.Clamp(baseSize + 1, 0, 5));
            player.Size = newSize;

            ApplyMgPenalty();
        }
        else
        {
            player.Size = (CreatureSize)baseSize;
            NwItem? offhand = player.GetItemInSlot(InventorySlot.LeftHand);
            if (offhand is not null)
            {
                player.ActionUnequipItem(offhand);
            }

            RemoveMgPenalty();
            player.ApplyEffect(EffectDuration.Instant, NWScript.EffectVisualEffect(2527));
            
        }
    }

    public bool IsMonkeyGripped()
    {
        NwItem? pcKey = player.FindItemWithTag("ds_pckey");

        if (pcKey is null) return false;

        int baseSize = NWScript.GetLocalInt(pcKey, "base_size");

        // Set the int if it hasn't been set...
        if (baseSize == 0)
        {
            baseSize = (int)player.Size;
            NWScript.SetLocalInt(pcKey, "base_size", baseSize);
        }

        return player.Size != (CreatureSize)baseSize;
    }

    public void ApplyMgPenalty()
    {
        Effect? existing = player.ActiveEffects.FirstOrDefault(effect => effect.Tag == "mg_penalty");
        if (existing is not null)
        {
            player.RemoveEffect(existing);
        }
        
        Effect mgPenalty = Effect.AttackDecrease(2);
        // mgPenalty = Effect.LinkEffects(Effect.SkillIncrease(NwSkill.FromSkillType(Skill.Hide)!, 4), mgPenalty);
        // mgPenalty = Effect.LinkEffects(Effect.SkillIncrease(NwSkill.FromSkillType(Skill.MoveSilently)!, 4), mgPenalty);
        // mgPenalty = Effect.LinkEffects(Effect.SkillIncrease(NwSkill.FromSkillType(Skill.Spot)!, 4), mgPenalty);
        // mgPenalty = Effect.LinkEffects(Effect.SkillIncrease(NwSkill.FromSkillType(Skill.Listen)!, 4), mgPenalty);
        // mgPenalty = Effect.LinkEffects(Effect.ACIncrease(1));
        mgPenalty.SubType = EffectSubType.Supernatural;
        mgPenalty.Tag = "mg_penalty";
        player.ApplyEffect(EffectDuration.Instant, NWScript.EffectVisualEffect(2527));

        player.ApplyEffect(EffectDuration.Permanent, mgPenalty);
        PlayerPlugin.UpdateCharacterSheet(player);
    }

    public void RemoveMgPenalty()
    {
        Effect? mgPenalty = player.ActiveEffects.FirstOrDefault(e => e.Tag == "mg_penalty");

        if (mgPenalty is not null)
        {
            player.RemoveEffect(mgPenalty);
            PlayerPlugin.UpdateCharacterSheet(player);
        }
    }
}