using System;

namespace AmiaReforged.PwEngine.Features.WindowingSystem.Scry;

/// <summary>
/// Opt-in contract for presenters that should automatically close when the owning player moves.
/// </summary>
public interface IAutoCloseOnMove : IScryPresenter
{
    /// <summary>
    /// Frequency to poll for movement. Defaults to one second.
    /// </summary>
    TimeSpan AutoClosePollInterval => TimeSpan.FromSeconds(1);

    /// <summary>
    /// Distance in meters a player can move before the window auto-closes. Defaults to 0.1m.
    /// </summary>
    float AutoCloseMovementThreshold => 0.1f;
}
