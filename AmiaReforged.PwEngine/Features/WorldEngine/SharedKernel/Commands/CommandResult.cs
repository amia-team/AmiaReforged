namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

/// <summary>
/// Result of executing a command.
/// </summary>
public sealed record CommandResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object>? Data { get; init; }

    /// <summary>
    /// Creates a successful command result.
    /// </summary>
    public static CommandResult Ok(Dictionary<string, object>? data = null) =>
        new() { Success = true, Data = data };

    /// <summary>
    /// Creates a failed command result with an error message.
    /// </summary>
    public static CommandResult Fail(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };

    /// <summary>
    /// Creates a successful command result with a single data value.
    /// </summary>
    public static CommandResult OkWith(string key, object value) =>
        new() { Success = true, Data = new Dictionary<string, object> { [key] = value } };
}

