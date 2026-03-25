namespace AmiaReforged.AdminPanel.Models;

/// <summary>
/// Result of deploying an entity (and its dependencies) from one endpoint to another.
/// </summary>
public class DeployResult
{
    /// <summary>The entity type that was deployed.</summary>
    public WorldEngineEntityType EntityType { get; set; }

    /// <summary>The key (tag / id) of the primary entity that was deployed.</summary>
    public string EntityKey { get; set; } = string.Empty;

    /// <summary>Import result for the primary entity.</summary>
    public ImportResult? EntityResult { get; set; }

    /// <summary>Import results for each dependency type that was deployed.</summary>
    public Dictionary<string, ImportResult> DependencyResults { get; set; } = new();

    /// <summary>Any top-level errors that prevented the deploy from completing.</summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>True when the primary entity and all dependencies succeeded without errors.</summary>
    public bool OverallSuccess =>
        Errors.Count == 0
        && EntityResult is { Failed: 0 }
        && DependencyResults.Values.All(r => r.Failed == 0);

    /// <summary>Total entities deployed across the primary entity and all dependencies.</summary>
    public int TotalDeployed =>
        (EntityResult?.Succeeded ?? 0)
        + DependencyResults.Values.Sum(r => r.Succeeded);
}
