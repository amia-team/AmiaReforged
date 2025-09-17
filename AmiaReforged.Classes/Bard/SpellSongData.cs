using Anvil.API;
using static AmiaReforged.Classes.Bard.SongConstants;

namespace AmiaReforged.Classes.Bard;

public readonly struct SpellSongData(byte requiredLevel, VfxType songSound)
{
    public byte RequiredLevel { get; } = requiredLevel;
    public VfxType SongSound { get; } = songSound;

    public static readonly Dictionary<NwSpell, SpellSongData> SongSpells = new()
    {
        {
            NwSpell.FromSpellType(SongConstants.BardSong)!,
            new SpellSongData(1, SfxBardSong)
        },
        {
            NwSpell.FromSpellType(Spell.AbilityEpicCurseSong)!,
            new SpellSongData(1, SfxCurseSong)
        }
    };
}
