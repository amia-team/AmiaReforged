using System;
using System.Collections.Generic;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.Characters;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.Characters.Entities;

public sealed class Persona : Entity
{
    private readonly HashSet<string> _unlocks = new(StringComparer.OrdinalIgnoreCase);

    // Natural identifier; normalize to provide a stable unique key. Empty for world-owned personas.
    public string Key { get; private set; } = string.Empty;

    // Ownership of this persona (World, DungeonMaster, Player).
    public Ownership Ownership { get; private set; } = Ownership.World();

    // Account-level unlocks (cosmetics, QoL features, etc.)
    public IReadOnlyCollection<string> Unlocks => _unlocks;

    private Persona(Ownership ownership, string? key)
    {
        Id = Guid.NewGuid();
        Ownership = ownership ?? throw new ArgumentNullException(nameof(ownership));

        if (Ownership.IsWorld)
        {
            // World-owned personas do not carry a key
            Key = string.Empty;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty for non-world personas.", nameof(key));
            Key = NormalizeKey(key);
        }

        LastUpdated = DateTime.UtcNow;
    }

    // Convenience: if key is empty/whitespace, treat as world-owned persona; otherwise, player-owned.
    public static Persona Create(string key)
        => string.IsNullOrWhiteSpace(key) ? CreateWorld() : CreatePlayer(key);

    public static Persona CreateWorld()
        => new(Ownership.World(), key: null);

    public static Persona CreatePlayer(string key)
    {
        string normalized = NormalizeKey(key);
        return new(Ownership.Player(new PlayerId(normalized)), normalized);
    }

    public static Persona CreateDungeonMaster(string key)
    {
        string normalized = NormalizeKey(key);
        return new(Ownership.DungeonMaster(new DmId(normalized)), normalized);
    }

    public static string NormalizeKey(string key) => (key ?? string.Empty).Trim().ToUpperInvariant();

    public bool HasUnlock(string unlockId)
    {
        if (string.IsNullOrWhiteSpace(unlockId)) return false;
        return _unlocks.Contains(unlockId.Trim());
    }

    public bool GrantUnlock(string unlockId)
    {
        if (string.IsNullOrWhiteSpace(unlockId)) return false;
        bool added = _unlocks.Add(unlockId.Trim());
        if (added) Touch();
        return added;
    }

    public bool RevokeUnlock(string unlockId)
    {
        if (string.IsNullOrWhiteSpace(unlockId)) return false;
        bool removed = _unlocks.Remove(unlockId.Trim());
        if (removed) Touch();
        return removed;
    }

    public void Touch() => LastUpdated = DateTime.UtcNow;
}
