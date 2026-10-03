using Anvil.API;
using Newtonsoft.Json;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Prototype;

/// <summary>Image actions use a matching left press/release; Click is diagnostic only.</summary>
public sealed class CodexImageInput
{
    private string? _pressedId;

    public bool Handle(NuiEventType eventType, string elementId, int? mouseButton, bool enabled)
    {
        if (eventType == NuiEventType.MouseDown)
        {
            _pressedId = mouseButton == 0 && enabled ? elementId : null;
            return false;
        }

        if (eventType != NuiEventType.MouseUp) return false;

        bool activate = mouseButton == 0 && enabled && _pressedId == elementId;
        _pressedId = null;
        return activate;
    }
}

public sealed class CodexMousePayload
{
    [JsonProperty("mouse_btn")]
    public int? MouseButton { get; init; }
}
