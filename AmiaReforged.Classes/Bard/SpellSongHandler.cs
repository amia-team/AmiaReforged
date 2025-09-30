using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;
using static AmiaReforged.Classes.Bard.SpellSongData;

namespace AmiaReforged.Classes.Bard;

[ServiceBinding(typeof(SpellSongHandler))]
public class SpellSongHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public SpellSongHandler()
    {
        NwModule.Instance.OnUseFeat += HandleSpellSong;
        NwModule.Instance.OnSpellCast += PlaySoundOnCast;

        Log.Info("Spell Song Handler initialized.");
    }

    private void HandleSpellSong(OnUseFeat eventData)
    {
        if (eventData.Feat.Spell is not { } spell || !SongSpells.TryGetValue(spell, out SpellSongData songData)) return;

        NwCreature bard = eventData.Creature;
        NwPlayer? player = bard.ControllingPlayer;

        if (bard.GetAbilityScore(Ability.Charisma, true) < songData.RequiredBaseCharisma)
        {
            player?.SendServerMessage($"Casting {spell.Name} requires base charisma {songData.RequiredBaseCharisma}.");
            eventData.PreventFeatUse = true;
            return;
        }

        if (bard.ActiveEffects.Any(e => e.EffectType == EffectType.Polymorph))
        {
            player?.SendServerMessage("Cannot cast songs while polymorphed.");
            eventData.PreventFeatUse = true;
            return;
        }

        // Return early for normal Bard Song, since that feat handles itself
        if (spell == NwSpell.FromSpellType(SongConstants.BardSong)) return;

        byte bardLevel = bard.GetClassInfo(ClassType.Bard)?.Level ?? 0;

        if (bardLevel == 0)
        {
            player?.SendServerMessage("You must be a bard to cast spell songs.");
            eventData.PreventFeatUse = true;
            return;
        }

        if (bardLevel < songData.RequiredLevel)
        {
            player?.SendServerMessage($"Casting {spell.Name} requires bard level {songData.RequiredLevel}.");
            eventData.PreventFeatUse = true;
            return;
        }

        NwFeat? bardSongFeat = NwFeat.FromFeatType(Feat.BardSongs);

        if (bardSongFeat == null || !bard.KnowsFeat(bardSongFeat))
        {
            player?.SendServerMessage("You need Bard Song to be able to cast spell songs.");
            eventData.PreventFeatUse = true;
            return;
        }

        if (NWScript.GetFeatRemainingUses(bardSongFeat.Id, bard) == 0)
        {
            player?.SendServerMessage("You're out of Bard Songs to cast spell songs.");
            eventData.PreventFeatUse = true;
            return;
        }

        bard.DecrementRemainingFeatUses(bardSongFeat);
    }

    private void PlaySoundOnCast(OnSpellCast eventData)
    {
        if (eventData.Spell == null || !SongSpells.TryGetValue(eventData.Spell, out SpellSongData songData)) return;

        NwCreature bard = (NwCreature)eventData.Caster;
        bard.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(songData.SongSound));
    }
}
