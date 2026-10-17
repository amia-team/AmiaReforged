using Anvil.Services;
namespace AmiaReforged.Core.Services.Sailing;

public enum SailingPersistenceBackend { Files, PostgreSql, Disabled }

/// <summary>Durable sailing files by default; PostgreSQL requires an explicitly selected, prepared schema.</summary>
[ServiceBinding(typeof(ShipPersistenceOptions))]
public sealed class ShipPersistenceOptions
{
    public bool Enabled { get; init; } = true;
    public SailingPersistenceBackend Backend { get; init; } = ParseBackend(
        Environment.GetEnvironmentVariable("AMIA_SAILING_PERSISTENCE"));
    public string? DataDirectory { get; init; } = Environment.GetEnvironmentVariable("AMIA_SAILING_DATA_DIRECTORY");

    public static SailingPersistenceBackend ParseBackend(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" or "files" => SailingPersistenceBackend.Files,
        "postgres" or "postgresql" => SailingPersistenceBackend.PostgreSql,
        "disabled" => SailingPersistenceBackend.Disabled,
        _ => throw new ArgumentException("AMIA_SAILING_PERSISTENCE must be files, postgres or disabled.")
    };
}
