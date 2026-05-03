using AmiaReforged.Classes.EffectUtils.ChangeAppearance;
using AmiaReforged.Classes.Warlock;
using AmiaReforged.Classes.Warlock.PactAppearance;
using AmiaReforged.Classes.Warlock.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Dark;

[ServiceBinding(typeof(IInvocation))]
public class WordOfChanging(ChangeAppearanceService changeAppearanceService) : IInvocation
{
    private const string WordOfChangingTag = "word_of_changing";
    private const VfxType FnfDoomOdd = (VfxType)2552;

    public string ImpactScript => "wlk_wordchange";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        Effect vfxDoomOdd = Effect.VisualEffect(FnfDoomOdd, fScale: 0.6f);

        Effect? wordOfChanging = warlock.ActiveEffects.FirstOrDefault(e => e.Tag == WordOfChangingTag);
        if (wordOfChanging != null)
        {
            warlock.RemoveEffect(wordOfChanging);
            // Reset invocation CL to warlock level, as this invocation decreases the invocation CL while active
            invocationCl = warlock.WarlockLevel();

            _ = ApplyWordOfChanging(warlock, invocationCl, delay: TimeSpan.FromSeconds(0.5), vfxApply: null, vfxRemove: vfxDoomOdd);
            return;
        }

        _ = ApplyWordOfChanging(warlock, invocationCl, delay: TimeSpan.Zero, vfxApply: vfxDoomOdd, vfxRemove: vfxDoomOdd);
    }

    private async Task ApplyWordOfChanging(NwCreature warlock, int invocationCl, TimeSpan delay, Effect? vfxApply, Effect? vfxRemove)
    {
        await NwTask.Delay(delay);
        await warlock.WaitForObjectContext();

        PactType? pactType = warlock.GetPact();
        if (pactType == null) return;

        ChangeAppearanceData? pactAppearance = PactAppearanceMap.GetAppearance(pactType.Value, warlock.Gender);
        if (pactAppearance == null) return;

        Effect? changeEffect = changeAppearanceService.EffectChangeAppearance(warlock, pactAppearance, vfxApply, vfxRemove);
        if (changeEffect == null) return;

        int abIncrease = Math.Min(5, invocationCl / 4);
        Effect wordOfChanging = Effect.LinkEffects
        (
            Effect.AttackIncrease(abIncrease),
            Effect.AbilityIncrease(Ability.Strength, Random.Shared.Roll(4)),
            Effect.AbilityIncrease(Ability.Dexterity, Random.Shared.Roll(4)),
            Effect.AbilityIncrease(Ability.Constitution, Random.Shared.Roll(4)),
            changeEffect
        );
        wordOfChanging.SubType = EffectSubType.Magical;
        wordOfChanging.Tag = WordOfChangingTag;

        TimeSpan duration = NwTimeSpan.FromRounds(invocationCl);

        warlock.ApplyEffect(EffectDuration.Temporary, wordOfChanging, duration);
    }
}
