using Anvil.API;
using Newtonsoft.Json;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui;

/// <summary>Image actions use a matching left press/release; Click never dispatches image actions.</summary>
public sealed class CodexImageInput
{
    private string? _pressedId;

    public void Reset() => _pressedId = null;

    public bool Handle(NuiEventType eventType, string elementId, int? mouseButton, bool enabled,
        bool isActionElement = true)
    {
        if (eventType == NuiEventType.MouseDown)
        {
            // NUI also emits MouseDown for decorative ancestor groups after the child.
            // Keep the child's capture; a release elsewhere still cancels it below.
            if (!isActionElement) return false;
            _pressedId = mouseButton == 0 && enabled ? elementId : null;
            return false;
        }

        if (eventType != NuiEventType.MouseUp) return false;

        bool activate = isActionElement && mouseButton == 0 && enabled && _pressedId == elementId;
        _pressedId = null;
        return activate;
    }
}

public sealed class CodexMousePayload
{
    [JsonProperty("mouse_btn")]
    public int? MouseButton { get; init; }
}
