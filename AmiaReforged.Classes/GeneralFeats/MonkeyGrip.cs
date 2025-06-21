using System.Reflection.PortableExecutable;
using Anvil.API;
using NLog;
using NLog.Fluent;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.GeneralFeats;

public class MonkeyGrip(NwCreature player)
{
    private const int MonkeyGripVisualEffect = 2527;

    public void ChangeSize()
    {
        int baseSize = GetBaseSize();

        bool shouldApplyMg = player.Size == (CreatureSize)baseSize;
        
        CreatureSize targetSize = shouldApplyMg 
            ? (CreatureSize)Math.Clamp(baseSize + 1, 0, 5)
            : (CreatureSize)baseSize;
        
        player.Size = targetSize;
        
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
        NwItem? pcKey = player.FindItemWithTag("ds_pckey");

        if (pcKey is null) return 0;

        int baseSize = NWScript.GetLocalInt(pcKey, "base_size");

        // Set the int if it hasn't been set...
        if (baseSize == 0)
        {
            baseSize = (int)player.Size;
            NWScript.SetLocalInt(pcKey, "base_size", baseSize);
        }

        return baseSize;
    }

    private void ApplyVisualEffect()
    {
        Effect? mgEffect = NWScript.EffectVisualEffect(MonkeyGripVisualEffect);
        if (mgEffect is null)
        {
            LogManager.GetCurrentClassLogger().Error("MonkeyGrip effect is null");
            return;
        }
            
        player.ApplyEffect(EffectDuration.Instant, mgEffect);
    }

    private void UnequipOffhand()
    {
        NwItem? offhand = player.GetItemInSlot(InventorySlot.LeftHand);
        if (offhand is not null)
        {
            player.ActionUnequipItem(offhand);
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
        mgPenalty.SubType = EffectSubType.Supernatural;
        mgPenalty.Tag = "mg_penalty";

        ApplyVisualEffect();

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