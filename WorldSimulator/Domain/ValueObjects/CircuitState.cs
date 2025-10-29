namespace WorldSimulator.Domain.ValueObjects;

/// <summary>
/// Represents the state of the circuit breaker
/// </summary>
public enum CircuitState
{
    /// <summary>Circuit is closed, traffic flows normally</summary>
    Closed = 0,

    /// <summary>Circuit is open, blocking all traffic due to failures</summary>
    Open = 1,

    /// <summary>Circuit is testing if service has recovered</summary>
    HalfOpen = 2
}

