namespace AmiaReforged.AdminPanel.Models;

/// <summary>
/// A named WorldEngine server endpoint that the admin panel can connect to.
/// Persisted to JSON on disk.
/// </summary>
public class WorldEngineEndpoint
{
    /// <summary>Unique identifier for this endpoint.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Display name shown in the server selector (e.g. "Production", "Test").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Base URL for the WorldEngine HTTP API (e.g. "http://nwserver:8080").</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Optional API key sent as an Authorization header with every request.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Whether this endpoint is currently enabled for use.</summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>Root object serialized to JSON for endpoint persistence.</summary>
public class WorldEngineEndpointState
{
    public List<WorldEngineEndpoint> Endpoints { get; set; } = new();
}
