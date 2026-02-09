using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.FickleStrand;

[ServiceBinding(typeof(IAugmentation))]
public class AugmentEmptyBody : IAugmentation.ICastAugment
{
    public PathType Path => PathType.FickleStrand;
    public TechniqueType Technique => TechniqueType.EmptyBody;

    /// <summary>
    /// Grants a Spell Mantle that absorbs 2d3 spell levels. Each Ki Focus adds +2d3 levels.
    /// </summary>
    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();

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
