namespace WorldSimulator.Infrastructure.PwEngineClient;

/// <summary>
/// Client for calling PwEngine WorldEngine REST API
/// </summary>
public interface IPwEngineClient
{
    /// <summary>
    /// Checks if the PwEngine service is healthy and reachable
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken ct = default);
}

