using AmiaReforged.Classes.Monk.Types;
using AmiaReforged.Classes.Spells;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.FickleStrand;

[ServiceBinding(typeof(IAugmentation))]
public class QuiveringPalm : IAugmentation.ICastAugment
{
    public PathType Path => PathType.FickleStrand;
    public TechniqueType Technique => TechniqueType.QuiveringPalm;
    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
    {
        AugmentQuiveringPalm(monk, castData);
    }

    /// <summary>
    /// Quivering Palm strips the enemy creature of a magical defense according to the breach list, with a 50% chance to
    /// steal the magical defense. Each Ki Focus adds an additional magical defense.
    /// </summary>
    private  void AugmentQuiveringPalm(NwCreature monk, OnSpellCast castData)
    {
        TouchAttackResult touchAttackResult = Techniques.Cast.QuiveringPalm.DoQuiveringPalm(monk, castData);

        if (castData.TargetObject is not NwCreature targetCreature || touchAttackResult is TouchAttackResult.Miss)
            return;

        int spellsToSteal = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        var stealableSpellGroups = targetCreature.ActiveEffects
            .Where(e => e.Spell != null && BreachList.BreachSpells.Contains(e.Spell.SpellType))
            .GroupBy(e => e.Spell)
            .Select(spellGroup => new
            {
                Spell = spellGroup.Key,
                Effects = spellGroup.ToList(),
                Duration = spellGroup.First().DurationRemaining
            })
            .Take(spellsToSteal)
            .ToArray();

        if (stealableSpellGroups.Length == 0)
        {
            monk.ControllingPlayer?.FloatingTextString("No magical defenses to steal!"
                .ColorString(ColorConstants.Purple));

            return;
        }

        List<string?> stolenSpellNames = [];
        foreach (var spellGroup in stealableSpellGroups)
        {
            if (Random.Shared.Roll(2) == 1)
            {
                stolenSpellNames.Add(spellGroup.Spell?.Name.ToString());

                foreach (Effect effect in spellGroup.Effects)
                {
                    monk.ApplyEffect(EffectDuration.Temporary, effect, TimeSpan.FromSeconds(spellGroup.Duration));
                }
            }

            foreach (Effect effect in spellGroup.Effects)
            {
                targetCreature.RemoveEffect(effect);
            }
        }

        if (stolenSpellNames.Count > 0)
        {
            string stolenMessageList = string.Join(", ", stolenSpellNames);
            string finalMessage = $"You successfully steal {stolenMessageList.ColorString(ColorConstants.Purple)}";
            monk.ControllingPlayer?.FloatingTextString(finalMessage);
        }

        targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpBreach));
    }
}
