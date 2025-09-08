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
    private static VfxType FnfCurseSong => (VfxType)2529;
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

        bard.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(FnfCurseSong));

        IEnumerable<NwCreature> hostileCreatures =
            bard.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Colossal, false)
            .Where(bard.IsReactionTypeHostile);

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

                ApplyCurseSong(foe, bard, eventData.Spell, curseSong, songDuration);

                continue;
            }

            ApplyCurseSong(foe, bard, eventData.Spell, curseSong, songDuration);

            if (songValues.Hp > 0)
                ApplyDamage(foe, songValues.Hp);
        }

        foreach (Effect effect in bard.ActiveEffects)
        {
            if (effect.EffectType is EffectType.VisualEffect && effect.IntParams[0] == (int)DurCurseSong)
                bard.RemoveEffect(effect);
        }

        bard.ApplyEffect(EffectDuration.Temporary, Effect.VisualEffect(DurCurseSong), songDuration);
    }

    private void ApplyCurseSong(NwCreature foe, NwCreature bard, NwSpell spell, Effect curseSong, TimeSpan songDuration)
    {
        foe.ApplyEffect(EffectDuration.Temporary, curseSong, songDuration);
        foe.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDoom));
        CreatureEvents.OnSpellCastAt.Signal(bard, foe, spell);
    }

    private void ApplyDamage(NwCreature foe, int damage)
    {
        foe.ApplyEffect(EffectDuration.Instant, Effect.Damage(damage, DamageType.Sonic));
        foe.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpSonic));
    }

    public void SetSpellResisted(bool result) { }
}
