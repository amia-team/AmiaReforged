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
    private static string CurseSongTag => "curse_song_";
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature bard) return;
        if (bard.Location == null) return;

        SongValues songValues = SongData.CalculateSongEffectValues(bard);

        Effect curseSong = Effect.LinkEffects
        (
            Effect.VisualEffect(VfxType.DurCessateNegative),
            Effect.AttackDecrease(songValues.Attack),
            Effect.DamageDecrease(songValues.Damage, DamageType.Slashing),
            Effect.SavingThrowDecrease(SavingThrow.Will, songValues.Will),
            Effect.SavingThrowDecrease(SavingThrow.Fortitude, songValues.Fortitude),
            Effect.SavingThrowDecrease(SavingThrow.Reflex, songValues.Reflex),
            Effect.ACDecrease(songValues.Ac),
            Effect.SkillDecreaseAll(songValues.Skill)
        );

        TimeSpan songDuration = NwTimeSpan.FromRounds(SongData.GetSongRounds(bard));

        int songPower = SongData.CalculateSongPower(songValues);
        string uniqueTag = CurseSongTag + songPower;

        curseSong.Tag = uniqueTag;
        curseSong.SubType = EffectSubType.Magical;

        bard.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfLosEvil30));

        IEnumerable<NwCreature> hostileCreatures =
            bard.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Colossal, false)
            .Where(bard.IsReactionTypeHostile);

        bard.ApplyEffect(EffectDuration.Temporary, Effect.VisualEffect(DurCurseSong), songDuration);

        int targetsAffected = 0;

        foreach (NwCreature foe in hostileCreatures)
        {
            if (foe.ActiveEffects.Any(e => e.EffectType is EffectType.Silence or EffectType.Deaf)) continue;

            Effect? existingCurseSong = foe.ActiveEffects.FirstOrDefault(e => e.Tag != null && e.Tag.StartsWith(CurseSongTag));

            if (existingCurseSong != null)
            {
                string songPowerString = existingCurseSong.Tag![CurseSongTag.Length..];

                if (int.TryParse(songPowerString, out int existingSongPower) &&
                    existingSongPower > songPower) continue;

                foe.RemoveEffect(existingCurseSong);
                foe.ApplyEffect(EffectDuration.Temporary, curseSong, songDuration);
                foe.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDoom));
                CreatureEvents.OnSpellCastAt.Signal(bard, foe, eventData.Spell);
                targetsAffected++;
                continue;
            }

            foe.ApplyEffect(EffectDuration.Temporary, curseSong, songDuration);
            foe.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDoom));
            CreatureEvents.OnSpellCastAt.Signal(bard, foe, eventData.Spell);
            targetsAffected++;

            if (songValues.Hp > 0)
                foe.ApplyEffect(EffectDuration.Instant, Effect.Damage(songValues.Hp, DamageType.Sonic));
        }

        if (targetsAffected > 0)
            bard.ApplyEffect(EffectDuration.Temporary, Effect.VisualEffect(DurCurseSong), songDuration);
    }

    public void SetSpellResisted(bool result) { }
}
