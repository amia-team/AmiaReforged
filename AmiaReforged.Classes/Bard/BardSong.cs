using AmiaReforged.Classes.Spells;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Bard;

[ServiceBinding(typeof(ISpell))]
public class BardSong : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S2_BardSong";
    private static string BardSongTag => "bard_song";
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature bard) return;
        if (bard.Location == null) return;

        SongValues songValues = SongData.CalculateSongEffectValues(bard);

        int songPower = SongData.CalculateSongPower(songValues);

        Effect bardSong = Effect.LinkEffects
        (
            Effect.VisualEffect(VfxType.DurBardSong),
            Effect.VisualEffect(VfxType.DurCessatePositive),
            Effect.AttackIncrease(songValues.Attack),
            Effect.DamageIncrease(songValues.Damage, DamageType.Bludgeoning),
            Effect.SavingThrowIncrease(SavingThrow.Will, songValues.Will),
            Effect.SavingThrowIncrease(SavingThrow.Fortitude, songValues.Fortitude),
            Effect.SavingThrowIncrease(SavingThrow.Reflex, songValues.Reflex),
            Effect.ACIncrease(songValues.Ac),
            Effect.SkillIncreaseAll(songValues.Skill),
            Effect.RunAction(data: $"{songPower}")
        );

        TimeSpan songDuration = NwTimeSpan.FromRounds(SongData.GetSongRounds(bard));

        bardSong.Tag = BardSongTag;
        bardSong.SubType = EffectSubType.Magical;

        bard.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfLosNormal30));

        IEnumerable<NwCreature> friendlyCreatures =
            bard.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Colossal, false)
            .Where(bard.IsReactionTypeFriendly);

        foreach (NwCreature ally in friendlyCreatures)
        {
            if (ally.ActiveEffects.Any(e => e.EffectType is EffectType.Silence or EffectType.Deaf)) continue;

            Effect? existingBardSong = ally.ActiveEffects.FirstOrDefault(e => e.Tag == BardSongTag);

            if (existingBardSong != null)
            {
                if (int.TryParse(existingBardSong.StringParams[0], out int existingSongPower) &&
                    existingSongPower > songPower) continue;

                ally.RemoveEffect(existingBardSong);
                ally.ApplyEffect(EffectDuration.Temporary, bardSong, songDuration);
                ally.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHeadSonic));
                continue;
            }

            ally.ApplyEffect(EffectDuration.Temporary, bardSong, songDuration);
            ally.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHeadSonic));

            if (songValues.Hp > 0)
                ally.ApplyEffect(EffectDuration.Temporary, Effect.TemporaryHitpoints(songValues.Hp), songDuration);
        }
    }

    public void SetSpellResisted(bool result) { }
}
