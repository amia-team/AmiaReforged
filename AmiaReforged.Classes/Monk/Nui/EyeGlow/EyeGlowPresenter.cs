using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Org.BouncyCastle.Crypto.Engines;
using static System.Enum;

namespace AmiaReforged.Classes.Monk.Nui.EyeGlow;

public sealed class EyeGlowPresenter(EyeGlowView view, NwPlayer player) : ScryPresenter<EyeGlowView>
{

    public override EyeGlowView View { get; } = view;
    public override NuiWindowToken Token() => _token;
    private EyeGlowModel Model { get; } = new();
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private const string WindowTitle = "Choose Your Eye Glow";

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType == NuiEventType.Click)
            HandleButtonClick(eventData);
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        NwCreature? monk = player.ControlledCreature;
        if (monk == null) return;

        if (eventData.ElementId.StartsWith("select_"))
            HandleSelectButtonClick(eventData, monk);

        if (eventData.ElementId.StartsWith("confirm_"))
        {
            HandleConfirmButtonClick(eventData, monk);
            RaiseCloseEvent();
        }
    }

    void HandleSelectButtonClick(ModuleEvents.OnNuiEvent eventData, NwCreature monk)
    {
        string selectedType = eventData.ElementId.Replace("select_", "");
        int token = eventData.Token.Token;

        foreach (EyeGlowType type in GetValues<EyeGlowType>())
        {
            string typeId = type.ToString();
            NuiBind<bool> bind = new($"visible_{typeId}");

            bool shouldBeVisible = (typeId == selectedType) && (selectedType != nameof(EyeGlowType.Remove));
            bind.SetBindValue(eventData.Player, token, shouldBeVisible);
        }

        if (!TryParse(selectedType, out EyeGlowType selectedEyeGlow))
        {
            player.FloatingTextString("Eye Glow not found, please file a bug report!", false);
            return;
        }

        AddTemporaryEyeGlow(monk, selectedEyeGlow);
    }

    private void AddTemporaryEyeGlow(NwCreature monk, EyeGlowType selectedEyeGlow)
    {
        foreach (Effect effect in monk.ActiveEffects)
        {
            if (effect.Tag == EyeGlowModel.TemporaryGlowTag)
                monk.RemoveEffect(effect);
        }

        VfxType? selectedVfx = Model.GetVfx(selectedEyeGlow, monk);
        if (selectedVfx == null) return;

        Effect eyeGlowVfx = Effect.VisualEffect(selectedVfx!);
        eyeGlowVfx.Tag = EyeGlowModel.TemporaryGlowTag;
        monk.ApplyEffect(EffectDuration.Temporary, eyeGlowVfx, NwTimeSpan.FromTurns(2));
    }

    private void HandleConfirmButtonClick(ModuleEvents.OnNuiEvent eventData, NwCreature monk)
    {
        string selectedType = eventData.ElementId.Replace("confirm_", "");

        if (!TryParse(selectedType, out EyeGlowType selectedEyeGlow))
        {
            player.FloatingTextString("Eye Glow not found, please file a bug report!", false);
            return;
        }

        AddPermanentEyeGlow(monk, selectedEyeGlow);
    }

    private void AddPermanentEyeGlow(NwCreature monk, EyeGlowType selectedEyeGlow)
    {
        foreach (Effect effect in monk.ActiveEffects)
        {
            if (effect.Tag == EyeGlowModel.TemporaryGlowTag)
                monk.RemoveEffect(effect);
        }

        VfxType? selectedVfx = Model.GetVfx(selectedEyeGlow, monk);
        if (selectedVfx == null) return;

        Effect eyeGlowVfx = Effect.VisualEffect(selectedVfx!);
        eyeGlowVfx.Tag = EyeGlowModel.PermanentGlowTag;
        eyeGlowVfx.SubType = EffectSubType.Unyielding;
        monk.ApplyEffect(EffectDuration.Permanent, eyeGlowVfx);

        player.FloatingTextString($"{selectedEyeGlow} eye glow added permanently!", false);
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), WindowTitle)
        {
            Geometry = new NuiRect(400f, 400f, 380f, 400f)
        };
    }

    public override void Create()
    {
        InitBefore();

        if (_window == null) return;

        player.TryCreateNuiWindow(_window, out _token);
    }

    public override void Close() => _token.Close();
}
