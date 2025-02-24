// Static bonus effects called by StaticBonusService
using Anvil.API;

namespace AmiaReforged.Classes.Monk.Effects;

public static class StaticBonusesEffect
{
    public static Effect GetStaticBonusesEffect(NwCreature monk)
    {   
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int wisMod = monk.GetAbilityModifier(Ability.Wisdom);
        
        int monkAcBonusAmount = monkLevel >= wisMod ? wisMod : monkLevel;
        Effect monkAcBonus = Effect.ACIncrease(monkAcBonusAmount, ACBonus.ShieldEnchantment);
        monkAcBonus.ShowIcon = false;

        int monkSpeedBonusAmount = monkLevel switch
        {
            >= 4 and <= 10 => 10,
            >= 11 and <= 16 => 20,
            >= 17 and <= 21 => 30,
            >= 22 and <= 26 => 40,
            >= 27 => 50,
            _ => 0
        };
        Effect monkSpeed = Effect.MovementSpeedIncrease(monkSpeedBonusAmount);
        monkSpeed.ShowIcon = false;

        int kiStrikeBonusAmount = monkLevel switch
        {
            >= 10 and <= 17 => 1,
            >= 18 and <= 25 => 2,
            >= 26 => 3,
            _ => 0
        };
        Effect kiStrike = Effect.AttackIncrease(kiStrikeBonusAmount);
        kiStrike.ShowIcon = false;

        Effect monkEffects = Effect.LinkEffects(monkAcBonus, monkSpeed, kiStrike);
        monkEffects.SubType = EffectSubType.Unyielding;
        monkEffects.Tag = "monk_staticeffects";
        return monkEffects;
    }
}