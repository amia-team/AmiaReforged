using AmiaReforged.Classes.Spells;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Bard;

[ServiceBinding(typeof(ISpell))]
public class CurseSong : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "X2_S2_CurseSong";
    private static VfxType DurCurseSong => (VfxType)507;
    private static string CurseSongTag => "curse_song";
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature bard) return;
        if (bard.Location == null) return;

        SongValues songValues = SongData.CalculateSongEffectValues(bard);

        int songPower = SongData.CalculateSongPower(songValues);

        Effect curseSong = Effect.LinkEffects
        (
            Effect.VisualEffect(DurCurseSong),
            Effect.VisualEffect(VfxType.DurCessateNegative),
            Effect.AttackDecrease(songValues.Attack),
            Effect.DamageDecrease(songValues.Damage, DamageType.Slashing),
            Effect.SavingThrowDecrease(SavingThrow.Will, songValues.Will),
            Effect.SavingThrowDecrease(SavingThrow.Fortitude, songValues.Fortitude),
            Effect.SavingThrowDecrease(SavingThrow.Reflex, songValues.Reflex),
            Effect.ACDecrease(songValues.Ac),
            Effect.SkillDecreaseAll(songValues.Skill),
            Effect.RunAction(data: $"{songPower}")
        );

        TimeSpan songDuration = NwTimeSpan.FromRounds(SongData.GetSongRounds(bard));

        curseSong.Tag = CurseSongTag;
        curseSong.SubType = EffectSubType.Magical;

        bard.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfLosEvil30));

        IEnumerable<NwCreature> hostileCreatures =
            bard.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Colossal, false)
            .Where(bard.IsReactionTypeHostile);

        foreach (NwCreature foe in hostileCreatures)
        {
            if (foe.ActiveEffects.Any(e => e.EffectType is EffectType.Silence or EffectType.Deaf)) continue;

            Effect? existingCurseSong = foe.ActiveEffects.FirstOrDefault(e => e.Tag == CurseSongTag);

            if (existingCurseSong != null)
            {
                if (int.TryParse(existingCurseSong.StringParams[0], out int existingSongPower) &&
                    existingSongPower > songPower) continue;

                foe.RemoveEffect(existingCurseSong);
                foe.ApplyEffect(EffectDuration.Temporary, curseSong, songDuration);
                foe.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDoom));
                CreatureEvents.OnSpellCastAt.Signal(bard, foe, eventData.Spell);
                continue;
            }

            foe.ApplyEffect(EffectDuration.Temporary, curseSong, songDuration);
            foe.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDoom));
            CreatureEvents.OnSpellCastAt.Signal(bard, foe, eventData.Spell);

            if (songValues.Hp > 0)
                foe.ApplyEffect(EffectDuration.Instant, Effect.Damage(songValues.Hp, DamageType.Sonic));
        }
    }

    public void SetSpellResisted(bool result) { }
}
