using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Emotes;

public sealed class EmoteTransformPresenter : ScryPresenter<EmoteTransformView>
{
    private readonly NwPlayer _player;
    private readonly NwCreature _targetCreature;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public EmoteTransformPresenter(EmoteTransformView view, NwPlayer player, NwCreature targetCreature)
    {
        View = view;
        _player = player;
        _targetCreature = targetCreature;
    }

    public override EmoteTransformView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), null!)
        {
            Geometry = new NuiRect(350f, 100f, 220f, 180f),
            Transparent = true,
            Resizable = false,
            Border = false,
            Collapsed = false,
            Closable = false,
        };
    }

    public override void Create()
    {
        if (_window == null)
        {
            _player.SendServerMessage(
                "The transform window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        // Get current transform values from the target creature
        float currentX = _targetCreature.VisualTransform.Translation.X;
        float currentY = _targetCreature.VisualTransform.Translation.Y;
        float currentZ = _targetCreature.VisualTransform.Translation.Z;

        // Set initial slider values
        Token().SetBindValue(View.TranslateX, currentX);
        Token().SetBindValue(View.TranslateY, currentY);
        Token().SetBindValue(View.TranslateZ, currentZ);

        // Set up watches for slider changes
        Token().SetBindWatch(View.TranslateX, true);
        Token().SetBindWatch(View.TranslateY, true);
        Token().SetBindWatch(View.TranslateZ, true);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        switch (obj.EventType)
        {
            case NuiEventType.Click:
                if (obj.ElementId == "btn_reset")
                {
                    ResetTransform();
                }
                break;

            case NuiEventType.Watch:
                // Update transform when sliders change
                float x = Token().GetBindValue(View.TranslateX);
                float y = Token().GetBindValue(View.TranslateY);
                float z = Token().GetBindValue(View.TranslateZ);

                ApplyTransform(x, y, z);
                break;
        }
    }

    private void ApplyTransform(float x, float y, float z)
    {
        _targetCreature.VisualTransform.Translation = new System.Numerics.Vector3(x, y, z);
    }

    private void ResetTransform()
    {
        // Get the saved Z from PC key
        string pcKey = _targetCreature.GetObjectVariable<LocalVariableString>("pc_key").Value ?? "";
        float savedZ = _targetCreature.GetObjectVariable<LocalVariableFloat>($"{pcKey}_emote_saved_z").Value;

        // Reset X and Y to 0, restore saved Z
        Token().SetBindValue(View.TranslateX, 0f);
        Token().SetBindValue(View.TranslateY, 0f);
        Token().SetBindValue(View.TranslateZ, savedZ);

        ApplyTransform(0f, 0f, savedZ);
    }

    public override void UpdateView()
    {
        // Nothing to update dynamically
    }

    public override void Close()
    {
        _token.Close();
    }
}
