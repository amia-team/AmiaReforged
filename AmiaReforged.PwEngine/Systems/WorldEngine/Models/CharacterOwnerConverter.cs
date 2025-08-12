using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Models;

public sealed class CharacterOwnerConverter() : ValueConverter<CharacterOwner, string>(ToProvider, FromProvider)
{
    private static readonly Expression<Func<CharacterOwner, string>> ToProvider = owner =>
        owner is CharacterOwner.Player ? $"player:{((CharacterOwner.Player)owner).Key}" :
        owner is CharacterOwner.DungeonMaster ? $"dm:{((CharacterOwner.DungeonMaster)owner).Key}" :
        owner is CharacterOwner.System ? $"system:{((CharacterOwner.System)owner).Key}" :
        "system:engine";

    private static readonly Expression<Func<string, CharacterOwner>> FromProvider = (value) =>
        Parse(value);

    private static CharacterOwner Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new CharacterOwner.System("engine");

        var idx = value.IndexOf(':');
        if (idx < 0) return new CharacterOwner.System(value);

        var type = value[..idx].ToLowerInvariant();
        var key = value[(idx + 1)..];

        return type switch
        {
            "player" => new CharacterOwner.Player(key),
            "dm" => new CharacterOwner.DungeonMaster(key),
            "system" => new CharacterOwner.System(key),
            _ => new CharacterOwner.System(key)
        };
    }
}