using Anvil.API;
using static AmiaReforged.Classes.Bard.SongConstants;

namespace AmiaReforged.Classes.Bard;

public readonly struct SpellSongData(byte requiredLevel, byte requiredBaseCharisma, VfxType songSound)
{
    public byte RequiredLevel { get; } = requiredLevel;
    public byte RequiredBaseCharisma { get; } = requiredBaseCharisma;
    public VfxType SongSound { get; } = songSound;

    public static readonly Dictionary<NwSpell, SpellSongData> SongSpells = new()
    {
        {
            NwSpell.FromSpellType(SongConstants.BardSong)!,
            new SpellSongData(1, 11, SfxBardSong)
        },
        {
            NwSpell.FromSpellType(Spell.AbilityEpicCurseSong)!,
            new SpellSongData(1, 11, SfxCurseSong)
        }
    };
}
