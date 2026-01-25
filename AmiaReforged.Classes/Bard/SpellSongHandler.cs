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
    private readonly NwFeat? _bardSongFeat = NwFeat.FromFeatType(Feat.BardSongs);

    public SpellSongHandler()
    {
        NwModule.Instance.OnUseFeat += HandleSpellSongFeat;
        NwModule.Instance.OnSpellCast += HandleSpellSongCast;

        Log.Info("Spell Song Handler initialized.");
    }

    private void HandleSpellSongFeat(OnUseFeat eventData)
    {
        if (eventData.Feat.Spell is not { } spell || !SongSpells.TryGetValue(spell, out SpellSongData songData)) return;

        NwCreature bard = eventData.Creature;
        NwPlayer? player = bard.ControllingPlayer;

        if (bard.ActiveEffects.Any(e => e.EffectType == EffectType.Polymorph))
        {
            player?.SendServerMessage("Cannot cast spell songs while polymorphed.");
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

        if (_bardSongFeat == null || !bard.KnowsFeat(_bardSongFeat))
        {
            player?.SendServerMessage("You need to know Bard Song to cast spell songs.");
            eventData.PreventFeatUse = true;
        }
    }

    private void HandleSpellSongCast(OnSpellCast eventData)
    {
        if (eventData.Spell == null || !SongSpells.TryGetValue(eventData.Spell, out SpellSongData songData)) return;

        NwCreature bard = (NwCreature)eventData.Caster;
        bard.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(songData.SongSound));

        if (bard.GetAbilityScore(Ability.Charisma, true) < songData.RequiredBaseCharisma)
        {
            bard.ControllingPlayer?.SendServerMessage($"Casting {eventData.Spell.Name} requires base charisma {songData.RequiredBaseCharisma}.");
            eventData.PreventSpellCast = true;
            return;
        }

        // Return early for normal Bard Song, since that feat handles itself
        if (eventData.Spell == NwSpell.FromSpellType(SongConstants.BardSong) || _bardSongFeat == null) return;

        if (NWScript.GetFeatRemainingUses(_bardSongFeat.Id, bard) == 0)
        {
            bard.ControllingPlayer?.SendServerMessage("You're out of Bard Songs to cast spell songs.");
            eventData.PreventSpellCast = true;
            return;
        }

        bard.DecrementRemainingFeatUses(_bardSongFeat);
    }
}
