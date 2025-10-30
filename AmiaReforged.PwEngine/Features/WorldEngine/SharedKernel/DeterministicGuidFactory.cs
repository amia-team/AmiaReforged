using System.Security.Cryptography;
using System.Text;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

/// <summary>
/// Helper for generating deterministic GUIDs from scoped string inputs.
/// Ensures repeatable identifiers when only stable string data is available.
/// </summary>
public static class DeterministicGuidFactory
{
    /// <summary>
    /// Creates a deterministic GUID based on the provided scope and value.
    /// </summary>
    /// <param name="scope">Logical namespace to avoid collisions between domains</param>
    /// <param name="value">Stable identifier within the scope</param>
    /// <returns>Deterministically generated GUID derived from the inputs</returns>
    public static Guid Create(string scope, string value)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentException("Scope cannot be null or whitespace", nameof(scope));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace", nameof(value));
        }

        string composite = $"{scope}:{value}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(composite));

        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);

        return new Guid(guidBytes);
    }
}
