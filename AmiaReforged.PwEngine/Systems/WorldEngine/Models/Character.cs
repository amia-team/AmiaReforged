using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Models;

public sealed class Character
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }
    public CharacterOwner Owner { get; private set; }

    private Character()
    {
    } // for serializers / ORMs if you share the class

    private Character(Guid id, string name, CharacterOwner owner)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));

        Id = id;
        Name = name;
        IsActive = true;
    }

    // Factories
    public static Character CreateForPlayer(string publicCdKey, string name)
        => new Character(Guid.NewGuid(), name, new CharacterOwner.Player(publicCdKey));

    public static Character CreateForDungeonMaster(string dmKey, string name)
        => new Character(Guid.NewGuid(), name, new CharacterOwner.DungeonMaster(dmKey));

    public static Character CreateForSystem(string name, string tag = "engine")
        => new Character(Guid.NewGuid(), name, new CharacterOwner.System(tag));

    // Transitions
    public void AssignToPlayer(string publicCdKey) => Owner = new CharacterOwner.Player(publicCdKey);
    public void AssignToDungeonMaster(string dmKey) => Owner = new CharacterOwner.DungeonMaster(dmKey);
    public void MakeSystemOwned(string tag = "engine") => Owner = new CharacterOwner.System(tag);

    // Typical behaviors
    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Name is required.", nameof(newName));
        Name = newName;
    }

    public void Deactivate() => IsActive = false;

    // Convenience checks
    public bool IsPlayerOwned(out string? playerKey)
    {
        if (Owner is CharacterOwner.Player p)
        {
            playerKey = p.Key;
            return true;
        }

        playerKey = null;
        return false;
    }

    public bool IsDmOwned(out string? dmKey)
    {
        if (Owner is CharacterOwner.DungeonMaster d)
        {
            dmKey = d.Key;
            return true;
        }

        dmKey = null;
        return false;
    }

    public bool IsSystemOwned(out string? tag)
    {
        if (Owner is CharacterOwner.System s)
        {
            tag = s.Key;
            return true;
        }

        tag = null;
        return false;
    }
}

public abstract record CharacterOwner
{
    private CharacterOwner()
    {
    }

    public sealed record Player(string PublicCdKey) : CharacterOwner
    {
        public string Key { get; } = string.IsNullOrWhiteSpace(PublicCdKey)
            ? throw new ArgumentException("Public CD Key is required.", nameof(PublicCdKey))
            : PublicCdKey;

        public override string ToString() => $"Player:{Key}";
    }

    public sealed record DungeonMaster(string DmKey) : CharacterOwner
    {
        public string Key { get; } = string.IsNullOrWhiteSpace(DmKey)
            ? throw new ArgumentException("DM Key is required.", nameof(DmKey))
            : DmKey;

        public override string ToString() => $"DM:{Key}";
    }

    public sealed record System(string Tag) : CharacterOwner
    {
        public string Key { get; } = string.IsNullOrWhiteSpace(Tag) ? "engine" : Tag;
        public override string ToString() => $"System:{Key}";
    }
}

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
