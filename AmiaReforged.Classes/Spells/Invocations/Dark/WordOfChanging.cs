using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Dark;

[ServiceBinding(typeof(IInvocation))]
public class WordOfChanging : IInvocation
{
    public string ImpactScript => "wlk_wordchange";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        int abIncrease = GetAttackBonusIncrease(warlock);

        Effect wordOfChanging = Effect.LinkEffects
        (
            Effect.AbilityIncrease(Ability.Strength, Random.Shared.Roll(4)),
            Effect.AbilityIncrease(Ability.Dexterity, Random.Shared.Roll(4)),
            Effect.AbilityIncrease(Ability.Constitution, Random.Shared.Roll(4)),
            Effect.AttackIncrease(abIncrease)
        );
        wordOfChanging.SubType = EffectSubType.Magical;

        TimeSpan duration = NwTimeSpan.FromRounds(invocationCl);

        warlock.ApplyEffect(EffectDuration.Temporary, wordOfChanging, duration);
    }

    private static int GetAttackBonusIncrease(NwCreature warlock)
    {
        int firstClassLevel = 0;
        int secondClassLevel = 0;
        int thirdClassLevel = 0;
        int fourthClassLevel = 0;

        // Ensure we don't go out of bounds of the actual character's level or the level info list
        int levelToCheck = Math.Min(warlock.Level, 20);

        for (int i = 0; i < levelToCheck; i++)
        {
            CreatureLevelInfo levelInfo = warlock.LevelInfo[i];

            if (warlock.Classes.Count > 0 && warlock.Classes[0].Class == levelInfo.ClassInfo.Class)
                firstClassLevel++;
            else if (warlock.Classes.Count > 1 && warlock.Classes[1].Class == levelInfo.ClassInfo.Class)
                secondClassLevel++;
            else if (warlock.Classes.Count > 2 && warlock.Classes[2].Class == levelInfo.ClassInfo.Class)
                thirdClassLevel++;
            else if (warlock.Classes.Count > 3 && warlock.Classes[3].Class == levelInfo.ClassInfo.Class)
                fourthClassLevel++;
        }

        // Access the 2DA Attack Bonus table for each class based on the calculated levels
        int firstClassAb = warlock.Classes.Count > 0 ? warlock.Classes[0].Class.AttackBonusTable[firstClassLevel] : 0;
        int secondClassAb = warlock.Classes.Count > 1 ? warlock.Classes[1].Class.AttackBonusTable[secondClassLevel] : 0;
        int thirdClassAb = warlock.Classes.Count > 2 ? warlock.Classes[2].Class.AttackBonusTable[thirdClassLevel] : 0;
        int fourthClassAb = warlock.Classes.Count > 3 ? warlock.Classes[3].Class.AttackBonusTable[fourthClassLevel] : 0;

        int totalBabAtLevel = firstClassAb + secondClassAb + thirdClassAb + fourthClassAb;

        int abIncrease = levelToCheck - totalBabAtLevel;

        string feedback = $"Base attack bonus at level {levelToCheck} is {totalBabAtLevel}." +
                          $"\nWord of Changing grants +{abIncrease} attack bonus.";
        warlock.ControllingPlayer?.SendServerMessage(feedback.AddWarlockColor());

        return abIncrease;
    }
}
