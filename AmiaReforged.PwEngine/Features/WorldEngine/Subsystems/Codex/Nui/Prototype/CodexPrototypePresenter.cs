using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Newtonsoft.Json;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Prototype;

public sealed class CodexPrototypePresenter(NwPlayer player) : ScryPresenter<CodexPrototypeView>
{
    private readonly NuiBind<NuiRect> _geometry = new("cdxp_geometry");
    private readonly CodexImageInput _imageInput = new();
    private NuiWindow _window = null!;
    private NuiWindowToken _token;
    private bool _categorySelected = true;
    private bool _entrySelected;
    private bool _entryEnabled = true;
    private int _actions;
    private int _clicks;
    private int _presses;
    private int _releases;

    public override CodexPrototypeView View { get; } = new();
    public override NuiWindowToken Token() => _token;

    public override void InitBefore() => _window = new NuiWindow(View.RootLayout(), null!)
    {
        Geometry = _geometry, Transparent = true, Border = false,
        Resizable = false, Closable = false, Collapsed = false
    };

    public override void Create()
    {
        if (!player.TryCreateNuiWindow(_window, out _token))
            throw new InvalidOperationException("The client could not create the Codex prototype window.");

        Position(-1, -1);
        _token.SetBindValue(View.Header,
            $"CODEX / 001 r5 - GUI {player.GetDeviceProperty(PlayerDeviceProperty.GuiWidth)}x" +
            $"{player.GetDeviceProperty(PlayerDeviceProperty.GuiHeight)} / " +
            $"{player.GetDeviceProperty(PlayerDeviceProperty.GuiScale)}%");
        _token.SetBindValue(View.Title, "A page from the Codex");
        _token.SetBindValue(View.Body, SampleText());
        _token.SetBindValue(View.Events, "Image events: press 0 / release 0 / click 0. Left-click a category or entry.");
        UpdateView();
        player.SendServerMessage($"Codex prototype: viewport {player.GetDeviceProperty(PlayerDeviceProperty.GuiWidth)} x " +
            $"{player.GetDeviceProperty(PlayerDeviceProperty.GuiHeight)}, GUI scale " +
            $"{player.GetDeviceProperty(PlayerDeviceProperty.GuiScale)}%. Repeat ./codex-ui to close.", ColorConstants.Cyan);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        bool image = ev.ElementId is "cdxp_category" or "cdxp_entry" or "cdxp_close";
        if (!image && ev.EventType is NuiEventType.MouseDown or NuiEventType.MouseUp)
            _imageInput.Handle(ev.EventType, ev.ElementId, null, false, isActionElement: false);

        if (image && ev.EventType is NuiEventType.MouseDown or NuiEventType.MouseUp or NuiEventType.Click)
        {
            int? button = null;
            string payloadStatus = "n/a";
            if (ev.EventType is NuiEventType.MouseDown or NuiEventType.MouseUp)
            {
                try
                {
                    button = ev.GetEventPayload<CodexMousePayload>()?.MouseButton;
                    payloadStatus = button?.ToString() ?? "missing";
                }
                catch (JsonException)
                {
                    payloadStatus = "invalid";
                }
            }

            if (ev.EventType == NuiEventType.MouseDown) _presses++;
            if (ev.EventType == NuiEventType.MouseUp) _releases++;
            if (ev.EventType == NuiEventType.Click) _clicks++;
            _token.SetBindValue(View.Events,
                $"Images: press {_presses} / release {_releases} / click {_clicks}. Last: {ev.ElementId} {ev.EventType}, button {payloadStatus}");

            bool enabled = ev.ElementId != "cdxp_entry" || _entryEnabled;
            if (!_imageInput.Handle(ev.EventType, ev.ElementId, button, enabled)) return;
            _actions++;
            switch (ev.ElementId)
            {
                case "cdxp_close":
                    RaiseCloseEvent();
                    return;
                case "cdxp_category":
                    _categorySelected = !_categorySelected;
                    break;
                case "cdxp_entry":
                    _entrySelected = !_entrySelected;
                    _token.SetBindValue(View.Title, $"Sample knowledge / activation {_actions}");
                    _token.SetBindValue(View.Body, SampleText());
                    break;
            }
            UpdateView();
            return;
        }

        // Native diagnostic buttons use Click only; their mouse events never dispatch actions.
        if (ev.EventType != NuiEventType.Click) return;
        switch (ev.ElementId)
        {
            case "cdxp_disable": _entryEnabled = !_entryEnabled; break;
            case "cdxp_center": Position(-1, -1); break;
            case "cdxp_corner": Position(24, 24); break;
            case "cdxp_reset":
                _actions = _clicks = _presses = _releases = 0;
                _token.SetBindValue(View.Events, "Image event counts reset.");
                break;
            default: return;
        }
        UpdateView();
    }

    public override void UpdateView()
    {
        _token.SetBindValue(View.CategoryTexture, _categorySelected ? "ui_cdx_cat_s" : "ui_cdx_cat_n");
        _token.SetBindValue(View.EntryTexture, _entrySelected ? "ui_cdx_ent_s" : "ui_cdx_ent_n");
        _token.SetBindValue(View.EntryEnabled, _entryEnabled);
        _token.SetBindValue(View.EntryColor, _entryEnabled ? new Color(242, 196, 113) : new Color(110, 100, 80));
        _token.SetBindValue(View.DisableLabel, _entryEnabled ? "Disable entry" : "Enable entry");
        _token.SetBindValue(View.Status, $"Image actions: {_actions} / entry {(_entryEnabled ? "enabled" : "disabled")}");
    }

    private void Position(float x, float y) => _token.SetBindValue(_geometry,
        new NuiRect(x, y, CodexPrototypeView.WindowWidth, CodexPrototypeView.WindowHeight));

    public override void Close() => _token.Close();

    private static string SampleText() =>
        "BEGIN OF SAMPLE\n\nThis is live NUI text on a separate parchment texture. " +
        "Check the title and this body independently: both should use dark brown ink.\n\n" +
        string.Join("\n\n", Enumerable.Range(1, 24).Select(i =>
            $"Passage {i:00}. A traveller records the roads, the old halls, and the knowledge gathered along the way. " +
            "The gold frame should stay still while this passage scrolls. Text should wrap before the parchment edge " +
            "and stay inside the reading pane, including when you scroll quickly.")) +
        "\n\nEND OF SAMPLE - passage 24 is complete. You should be able to read this entire final line.";
}
