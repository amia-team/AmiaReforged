namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// Exception thrown when the WorldEngine API returns an error.
/// Shared across all WorldEngine API service classes.
/// </summary>
public class WorldEngineApiException : Exception
{
    public int StatusCode { get; }
    public string ErrorTitle { get; }
    public string Detail { get; }

    public WorldEngineApiException(int statusCode, string errorTitle, string? detail)
        : base($"[{statusCode}] {errorTitle}: {detail}")
    {
        StatusCode = statusCode;
        ErrorTitle = errorTitle;
        Detail = detail ?? string.Empty;
    }
}
