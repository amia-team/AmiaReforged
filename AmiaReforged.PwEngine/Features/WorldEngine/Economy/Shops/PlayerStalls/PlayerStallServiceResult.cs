using System;
using System.Collections.Generic;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;

/// <summary>
/// Outcome of a player stall service operation.
/// </summary>
public sealed class PlayerStallServiceResult
{
    private PlayerStallServiceResult(
        bool success,
        PlayerStallError error,
        string? errorMessage,
    IReadOnlyDictionary<string, object>? data)
    {
        Success = success;
        Error = error;
        ErrorMessage = errorMessage;
        Data = data;
    }

    public bool Success { get; }

    public PlayerStallError Error { get; }

    public string? ErrorMessage { get; }

    public IReadOnlyDictionary<string, object>? Data { get; }

    public static PlayerStallServiceResult Ok(IReadOnlyDictionary<string, object>? data = null)
    {
        return new PlayerStallServiceResult(true, PlayerStallError.None, null, data);
    }

    public static PlayerStallServiceResult Fail(PlayerStallError error, string message)
    {
        if (error is PlayerStallError.None)
        {
            throw new ArgumentException("Failure result must specify an error code.", nameof(error));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Error message is required for failures.", nameof(message));
        }

        return new PlayerStallServiceResult(false, error, message, null);
    }
}
