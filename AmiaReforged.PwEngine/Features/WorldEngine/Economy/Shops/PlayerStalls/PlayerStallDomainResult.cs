using System;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;

/// <summary>
/// Represents the outcome of a domain-level player stall operation.
/// </summary>
/// <typeparam name="TPayload">Type of the payload returned on success.</typeparam>
public sealed class PlayerStallDomainResult<TPayload>
{
    private PlayerStallDomainResult(bool success, PlayerStallError error, string? errorMessage, TPayload? payload)
    {
        Success = success;
        Error = error;
        ErrorMessage = errorMessage;
        Payload = payload;
    }

    public bool Success { get; }

    public PlayerStallError Error { get; }

    public string? ErrorMessage { get; }

    public TPayload? Payload { get; }

    public static PlayerStallDomainResult<TPayload> Ok(TPayload payload)
    {
        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        return new PlayerStallDomainResult<TPayload>(true, PlayerStallError.None, null, payload);
    }

    public static PlayerStallDomainResult<TPayload> Fail(PlayerStallError error, string message)
    {
        if (error is PlayerStallError.None)
        {
            throw new ArgumentException("Failure result must specify an error code.", nameof(error));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Error message is required for failure results.", nameof(message));
        }

        return new PlayerStallDomainResult<TPayload>(false, error, message, default);
    }
}
