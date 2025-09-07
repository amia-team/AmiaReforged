using Anvil.API;

namespace AmiaReforged.Classes.Associates;

public class FamiliarBonuses(NwCreature owner, NwCreature associate)
{
    private readonly int _sorcererLevel = owner.GetClassInfo(ClassType.Sorcerer)?.Level ?? 0;
    private readonly int _wizardLevel = owner.GetClassInfo(ClassType.Wizard)?.Level ?? 0;
    private int BonusLevel => _sorcererLevel + _wizardLevel;

    public void ApplyFamiliarBonus()
    {
        associate.ApplyEffect(EffectDuration.Permanent, FamiliarEffect());

        NwItem? mainHand = associate.GetItemInSlot(InventorySlot.RightHand);
        NwItem? leftClaw = associate.GetItemInSlot(InventorySlot.CreatureLeftWeapon);
        NwItem? rightClaw = associate.GetItemInSlot(InventorySlot.CreatureRightWeapon);
        NwItem? bite = associate.GetItemInSlot(InventorySlot.CreatureBiteWeapon);

        if (mainHand != null)
            mainHand.AddItemProperty(EnhancementBonus().ItemProperty, EffectDuration.Permanent);
        else
        {
            if (leftClaw != null)
                leftClaw.AddItemProperty(EnhancementBonus().ItemProperty, EffectDuration.Permanent);
            if (rightClaw != null)
                rightClaw.AddItemProperty(EnhancementBonus().ItemProperty, EffectDuration.Permanent);
            if (bite != null)
                bite.AddItemProperty(EnhancementBonus().ItemProperty, EffectDuration.Permanent);
        }

        if (owner.IsPlayerControlled(out NwPlayer? player))
            SendFamiliarBonusFeedback(player);
    }

    private Effect FamiliarEffect()
    {
        Effect companionBonus = Effect.LinkEffects(
            NaturalArmorBonus().Effect,
            RegenerateBonus().Effect,
            AttackBonus().Effect,
            UniversalSaveBonus().Effect,
            DamageResistanceBonus().Effect,
            Effect.CutsceneGhost());

        companionBonus.SubType = EffectSubType.Unyielding;

        return companionBonus;
    }

    private (Effect Effect, int Bonus) NaturalArmorBonus()
    {
        int acBonus = BonusLevel / 2;

        return (Effect.ACIncrease(acBonus, ACBonus.Natural), acBonus);
    }

    private (Effect Effect, int Bonus) RegenerateBonus()
    {
        int regenBonus = BonusLevel / 5 == 0 ? 1 : BonusLevel / 5;

        return (Effect.Regenerate(regenBonus, NwTimeSpan.FromRounds(1)), regenBonus);
    }

    private (Effect Effect, int Bonus) AttackBonus()
    {
        int attackBonus = BonusLevel / 5;

        return (Effect.AttackIncrease(attackBonus), attackBonus);
    }

    private (Effect Effect, int Bonus) UniversalSaveBonus()
    {
        int saveBonus = BonusLevel / 5;

        return (Effect.SavingThrowIncrease(SavingThrow.All, saveBonus), saveBonus);
    }

    private (Effect Effect, int Bonus) DamageResistanceBonus()
    {
        int damageResistanceBonus = BonusLevel switch
        {
            >= 21 => 15,
            >= 11 => 10,
            _ => 5
        };

        Effect damageResistance =
            Effect.LinkEffects(Effect.DamageResistance(DamageType.Bludgeoning, damageResistanceBonus),
                Effect.DamageResistance(DamageType.Piercing, damageResistanceBonus),
                Effect.DamageResistance(DamageType.Slashing, damageResistanceBonus));

        return (damageResistance, damageResistanceBonus);
    }

    private (ItemProperty ItemProperty, int Bonus) EnhancementBonus()
    {
        int enhancementBonus = BonusLevel / 5;

        return (ItemProperty.EnhancementBonus(enhancementBonus), enhancementBonus);
    }

    private void SendFamiliarBonusFeedback(NwPlayer player)
    {
        string feedback =
            $"Familiar Bonuses Added:\n" +
            $"Natural AC +{NaturalArmorBonus().Bonus}\n" +
            $"Weapon Enhancement +{EnhancementBonus().Bonus}\n" +
            $"Regeneration +{RegenerateBonus().Bonus}\n" +
            $"Attack Bonus +{AttackBonus().Bonus}\n" +
            $"Universal Save +{UniversalSaveBonus().Bonus}\n" +
            $"Physical Resistance +{DamageResistanceBonus().Bonus}";

        player.SendServerMessage(feedback.ColorString(ColorConstants.Magenta));
    }
}
