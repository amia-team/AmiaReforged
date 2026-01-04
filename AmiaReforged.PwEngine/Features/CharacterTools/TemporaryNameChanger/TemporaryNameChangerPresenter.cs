using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.CharacterTools.TemporaryNameChanger;

public sealed class TemporaryNameChangerPresenter(TemporaryNameChangerView view, NwPlayer player)
    : ScryPresenter<TemporaryNameChangerView>
{
    public override TemporaryNameChangerView View { get; } = view;

    private readonly TemporaryNameChangerModel _model = new(player);
    private NuiWindowToken _token;

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
    }

    public override void Create()
    {
        NuiWindow window = new NuiWindow(View.RootLayout(), "Temporary Name Changer")
        {
            Geometry = new NuiRect(50f, 50f, 675f, 350f),
            Resizable = true
        };

        if (!player.TryCreateNuiWindow(window, out _token))
            return;

        InitializeBindValues();
    }

    public override void Close()
    {
        _token.Close();
    }

    private void InitializeBindValues()
    {
        // Enable all controls
        Token().SetBindValue(View.AlwaysEnabled, true);
        Token().SetBindValue(View.TempNameConfirmEnabled, true);

        // Clear the input field
        Token().SetBindValue(View.TempNameText, "");
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType != NuiEventType.Click) return;

        switch (eventData.ElementId)
        {
            case "btn_tempname_confirm":
                string tempName = Token().GetBindValue(View.TempNameText) ?? "";
                if (!string.IsNullOrWhiteSpace(tempName))
                {
                    _model.SetTemporaryName(tempName);
                    Token().SetBindValue(View.TempNameText, ""); // Clear the input field after successful change
                }
                break;

            case "btn_restore_name":
                _model.RestoreOriginalName();
                Token().SetBindValue(View.TempNameText, ""); // Clear the input field
                break;

            case "btn_close":
                Close();
                break;
        }
    }
}

