using System.Security.Cryptography;
using System.Text;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

/// <summary>
/// Value object representing a unique DM identifier.
/// Provides deterministic conversion from NWN public CD keys to GUIDs,
/// allowing DMs to have persistent codex entries regardless of character used.
/// </summary>
public readonly record struct DmId
{
    public Guid Value { get; }

    private DmId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("DmId cannot be empty", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Creates a DmId from an existing GUID.
    /// </summary>
    public static DmId From(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("DmId cannot be empty", nameof(id));

        return new DmId(id);
    }

    /// <summary>
    /// Creates a deterministic DmId from a NWN public CD key.
    /// The same CD key will always produce the same GUID, ensuring
    /// DMs have persistent identities across different characters/sessions.
    /// </summary>
    /// <param name="publicCdKey">The 8-character NWN public CD key</param>
    /// <returns>A deterministic DmId based on the CD key</returns>
    public static DmId FromCdKey(string publicCdKey)
    {
        if (string.IsNullOrWhiteSpace(publicCdKey))
            throw new ArgumentException("Public CD key cannot be null or whitespace", nameof(publicCdKey));

        // Normalize: uppercase and trim
        string normalized = publicCdKey.Trim().ToUpperInvariant();

        // Validate format (typically 8 alphanumeric characters for NWN)
        if (normalized.Length != 8)
            throw new ArgumentException(
                $"Public CD key must be exactly 8 characters, got {normalized.Length}",
                nameof(publicCdKey));

        // Create deterministic GUID from CD key using SHA256
        // This ensures the same CD key always produces the same GUID
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));

        // Take first 16 bytes of hash to create GUID
        byte[] guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);

        Guid deterministicGuid = new Guid(guidBytes);

        return new DmId(deterministicGuid);
    }

    /// <summary>
    /// Implicit conversion from DmId to Guid for backward compatibility.
    /// </summary>
    public static implicit operator Guid(DmId dmId) => dmId.Value;

    /// <summary>
    /// Explicit conversion from Guid to DmId (requires validation).
    /// </summary>
    public static explicit operator DmId(Guid id) => From(id);

    /// <summary>
    /// Implicit conversion from DmId to CharacterId.
    /// DMs can have codices just like characters.
    /// </summary>
    public static implicit operator CharacterId(DmId dmId) => CharacterId.From(dmId.Value);

    /// <summary>
    /// Explicit conversion from CharacterId to DmId.
    /// Allows treating DM codices polymorphically with character codices.
    /// </summary>
    public static explicit operator DmId(CharacterId characterId) => From(characterId.Value);

    public override string ToString() => Value.ToString();
}
