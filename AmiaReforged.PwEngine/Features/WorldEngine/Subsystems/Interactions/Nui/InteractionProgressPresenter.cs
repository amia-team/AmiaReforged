using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Nui;

/// <summary>
/// Presenter for the interaction progress bar popup. Runs a timed loop that calls
/// <see cref="IInteractionSubsystem.PerformInteractionAsync"/> every 6 seconds (1 NWN round),
/// updating the progress bar each tick. Auto-closes on completion, failure, or cancellation.
/// <para>
/// Does NOT implement <see cref="IAutoCloseOnMove"/> — Glyph pipeline scripts handle
/// distance checks independently.
/// </para>
/// </summary>
public sealed class InteractionProgressPresenter : ScryPresenter<InteractionProgressView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>Seconds per tick — 1 NWN round = 6 seconds.</summary>
    private const int SecondsPerTick = 6;

    /// <summary>Delay in seconds before the popup auto-closes after completion/failure.</summary>
    private const int AutoClosDelaySeconds = 2;

    /// <summary>Maximum ticks to guard against runaway loops.</summary>
    private const int MaxTicks = 100;

    private readonly NwPlayer _player;
    private readonly string _interactionTag;
    private readonly CharacterId _characterId;
    private readonly Guid _targetId;
    private readonly string? _areaResRef;

    private NuiWindowToken _token;
    private NuiWindow? _window;
    private bool _cancelled;

    [Inject] private Lazy<IInteractionSubsystem>? InteractionSubsystem { get; init; }

    public InteractionProgressPresenter(
        InteractionProgressView view,
        NwPlayer player,
        string interactionTag,
        CharacterId characterId,
        Guid targetId,
        string? areaResRef)
    {
        View = view;
        _player = player;
        _interactionTag = interactionTag;
        _characterId = characterId;
        _targetId = targetId;
        _areaResRef = areaResRef;
    }

    public override InteractionProgressView View { get; }
    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _cancelled = false;

        _window = new NuiWindow(View.RootLayout(), $"Interaction: {_interactionTag}")
        {
            Geometry = new NuiRect(
                -1f, -1f,
                InteractionProgressView.WindowW,
                InteractionProgressView.WindowH),
            Closable = false,
            Resizable = false,
            Collapsed = false,
            Border = true
        };
    }

    public override void Create()
    {
        if (_window == null)
        {
            _player.SendServerMessage("Interaction progress window not configured.", ColorConstants.Orange);
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
        {
            _player.SendServerMessage("Unable to open interaction progress window.", ColorConstants.Orange);
            return;
        }

        // Set initial bind values
        _token.SetBindValue(View.StatusText, "Starting...");
        _token.SetBindValue(View.ProgressValue, 0f);
        _token.SetBindValue(View.RoundsRemainingText, "");

        // Start the tick loop
        StartTickLoop();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        // No interactive elements — the window auto-closes.
    }

    public override void Close()
    {
        _cancelled = true;
        try { _token.Close(); } catch { /* token may already be invalid */ }
    }

    // ──────────────────────────────────────────────────────────
    //  Tick Loop
    // ──────────────────────────────────────────────────────────

    private void StartTickLoop()
    {
        _ = NwTask.Run(async () =>
        {
            IInteractionSubsystem interactions = InteractionSubsystem!.Value;

            for (int tick = 0; tick < MaxTicks; tick++)
            {
                // Guard: player disconnected or window was closed
                if (_cancelled || !_player.IsValid) return;

                // ─── Perform one tick ───
                CommandResult result;
                try
                {
                    result = await interactions.PerformInteractionAsync(
                        _characterId, _interactionTag, _targetId, _areaResRef);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error during interaction tick for '{Tag}'", _interactionTag);
                    await ShowResultAndClose("Error", "An unexpected error occurred.");
                    return;
                }

                await NwTask.SwitchToMainThread();
                if (_cancelled || !_player.IsValid) return;

                // ─── Handle result ───
                if (!result.Success)
                {
                    // Interaction failed or was cancelled by Glyph script
                    await ShowResultAndClose("Failed", result.ErrorMessage ?? "Interaction failed.");
                    return;
                }

                string status = result.Data?.TryGetValue("status", out object? s) == true
                    ? s?.ToString() ?? ""
                    : "";

                if (status is "Completed" or "Failed")
                {
                    bool success = status == "Completed";
                    string message = success ? "Complete!" : "Failed";
                    Color color = success ? ColorConstants.Lime : ColorConstants.Red;

                    _token.SetBindValue(View.ProgressValue, 1.0f);
                    _token.SetBindValue(View.StatusText, message);
                    _token.SetBindValue(View.RoundsRemainingText, "");

                    _player.SendServerMessage(
                        $"Interaction '{_interactionTag}' {message.ToLowerInvariant()}",
                        color);

                    await NwTask.Delay(TimeSpan.FromSeconds(AutoClosDelaySeconds));
                    await NwTask.SwitchToMainThread();
                    if (!_cancelled) { RaiseCloseEvent(); Close(); }
                    return;
                }

                // ─── Update progress bar ───
                InteractionInfo? info = interactions.GetActiveInteraction(_characterId);
                if (info != null)
                {
                    float progress = info.RequiredRounds > 0
                        ? info.Progress / (float)info.RequiredRounds
                        : 0f;
                    int remaining = info.RequiredRounds - info.Progress;

                    _token.SetBindValue(View.ProgressValue, Math.Clamp(progress, 0f, 1f));
                    _token.SetBindValue(View.StatusText, FormatStatusText(info));
                    _token.SetBindValue(View.RoundsRemainingText,
                        remaining > 0 ? $"{remaining} round{(remaining != 1 ? "s" : "")} remaining" : "");
                }

                // ─── Wait for next tick (1 NWN round = 6 seconds) ───
                await NwTask.Delay(TimeSpan.FromSeconds(SecondsPerTick));
                await NwTask.SwitchToMainThread();
            }

            // Safety limit
            if (!_cancelled && _player.IsValid)
            {
                _player.SendServerMessage(
                    "Interaction progress: safety limit reached — stopping.",
                    ColorConstants.Orange);
                RaiseCloseEvent();
                Close();
            }
        });
    }

    /// <summary>
    /// Shows an end-state message in the progress bar and auto-closes after a brief delay.
    /// </summary>
    private async Task ShowResultAndClose(string label, string message)
    {
        await NwTask.SwitchToMainThread();
        if (_cancelled || !_player.IsValid) return;

        _token.SetBindValue(View.StatusText, label);
        _token.SetBindValue(View.RoundsRemainingText, message);

        _player.SendServerMessage(
            $"Interaction '{_interactionTag}': {message}",
            label == "Failed" ? ColorConstants.Red : ColorConstants.Orange);

        await NwTask.Delay(TimeSpan.FromSeconds(AutoClosDelaySeconds));
        await NwTask.SwitchToMainThread();
        if (!_cancelled) { RaiseCloseEvent(); Close(); }
    }

    private static string FormatStatusText(InteractionInfo info)
    {
        // Capitalize first letter of the tag for display
        string display = info.InteractionTag;
        if (display.Length > 0)
        {
            display = char.ToUpper(display[0]) + display[1..];
        }

        // Replace underscores with spaces for readability
        display = display.Replace('_', ' ');

        return $"{display}...";
    }
}
