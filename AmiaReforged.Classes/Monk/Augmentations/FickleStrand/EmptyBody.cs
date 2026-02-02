using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.FickleStrand;

[ServiceBinding(typeof(IAugmentation))]
public class EmptyBody : IAugmentation.ICastAugment
{
    public PathType Path => PathType.FickleStrand;
    public TechniqueType Technique => TechniqueType.EmptyBody;
    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();
        AugmentEmptyBody(monk);
    }

    /// <summary>
    /// Empty Body grants a spell mantle that absorbs up to 2 spells and spell-like abilities.
    /// Each Ki Focus increases the effects it can absorb by 2, to a maximum of 8 spells or spell-like abilities.
    /// </summary>
    private void AugmentEmptyBody(NwCreature monk)
    {
        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        byte diceAmount = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 4,
            KiFocus.KiFocus2 => 6,
            KiFocus.KiFocus3 => 8,
            _ => 2
        };

        int totalSpellsAbsorbed = Random.Shared.Roll(3, diceAmount);

        Effect spellAbsorb = Effect.LinkEffects(
            Effect.SpellLevelAbsorption(9, totalSpellsAbsorbed),
            Effect.VisualEffect(VfxType.DurSpellturning)
        );
        spellAbsorb.SubType = EffectSubType.Extraordinary;

        monk.ApplyEffect(EffectDuration.Temporary, spellAbsorb, NwTimeSpan.FromRounds(monkLevel));
    }
}
