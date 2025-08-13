namespace AmiaReforged.PwEngine.Systems.WorldEngine.Models;

public sealed class Character
{
    public Guid Id { get; set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }
    public CharacterOwner Owner { get; private set; }

    public Character()
    {
    }

    public Character(Guid id, string name, CharacterOwner owner)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));

        Id = id;
        Name = name;
        IsActive = true;
    }

    public static Character CreateEmpty() => new();
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

    public void Activate()
    {
        IsActive = true;
    }
}
