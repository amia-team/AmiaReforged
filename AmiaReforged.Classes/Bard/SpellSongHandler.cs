using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.Classes.Bard;

[ServiceBinding(typeof(SpellSongHandler))]
public class SpellSongHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public SpellSongHandler()
    {
        NwModule.Instance.OnSpellCast += HandleSpellSong;

        Log.Info("Spell Song Handler initialized.");
    }

    private static readonly Dictionary<NwSpell, byte> SongSpellByLevel = new()
    {
        { NwSpell.FromSpellType(Spell.AbilityEpicCurseSong)!, 1 }
    };

    private void HandleSpellSong(OnSpellCast eventData)
    {
        if (eventData.Spell is not { } spell) return;
        if (eventData.Caster is not NwCreature bard) return;
        if (!SongSpellByLevel.TryGetValue(spell, out byte requiredBardLevel)) return;

        byte bardLevel = bard.GetClassInfo(ClassType.Bard)?.Level ?? 0;
        NwPlayer? player = bard.ControllingPlayer;

        NwFeat? bardSongFeat = NwFeat.FromFeatType(Feat.BardSongs);

        if (bardLevel == 0)
        {
            player?.SendServerMessage("You must be a bard to cast spell songs.");
            eventData.PreventSpellCast = true;
            return;
        }

        if (bardSongFeat == null || !bard.KnowsFeat(bardSongFeat))
        {
            player?.SendServerMessage("You need Bard Song to be able to cast spell songs.");
            eventData.PreventSpellCast = true;
            return;
        }

        if (NWScript.GetFeatRemainingUses(bardSongFeat.Id, bard) == 0)
        {
            player?.SendServerMessage("You're out of Bard Songs to cast spell songs.");
            eventData.PreventSpellCast = true;
            return;
        }

        if (bardLevel < requiredBardLevel)
        {
            player?.SendServerMessage($"Casting {spell.Name} requires bard level {requiredBardLevel}.");
            eventData.PreventSpellCast = true;
            return;
        }

        bard.DecrementRemainingFeatUses(bardSongFeat);
    }
}
