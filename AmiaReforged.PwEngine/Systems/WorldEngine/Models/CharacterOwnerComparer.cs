using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Models;

public sealed class CharacterOwnerComparer() : ValueComparer<CharacterOwner>((a, b) => ToKey(a) == ToKey(b),
    v => ToKey(v).GetHashCode(),
    v => v)
{
    // immutable record, shallow copy is fine

    private static string ToKey(CharacterOwner v) => v switch
    {
        CharacterOwner.Player p => $"player:{p.Key}",
        CharacterOwner.DungeonMaster d => $"dm:{d.Key}",
        CharacterOwner.System s => $"system:{s.Key}",
        _ => "system:engine"
    };
}