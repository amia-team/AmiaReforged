using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.ItemTool;

public sealed class ItemToolPresenter : ScryPresenter<ItemToolView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private readonly ItemToolModel _model;

    private bool _initializing;

    public ItemToolPresenter(ItemToolView view, NwPlayer player)
    {
        View = view;
        _player = player;
        _model = new ItemToolModel(player);
        _model.OnNewSelection += OnNewSelection;
    }

    public override ItemToolView View { get; }
    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(520f, 140f, 520f, 535f),
            Resizable = false
        };
    }

    public override void Create()
    {
        if (_window == null) InitBefore();

        if (_window == null)
        {
            _player.SendServerMessage(
                "The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        // Seed UI from model and then start watches
        UpdateFromModel();
        Token().SetBindWatch(View.Name, true);
        Token().SetBindWatch(View.Description, true);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        switch (ev.EventType)
        {
            case NuiEventType.Click:
                HandleClick(ev);
                break;
            case NuiEventType.Watch:
                HandleWatch(ev.ElementId);
                break;
        }
    }

    private void HandleClick(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.ElementId == View.SelectItemButton.Id)
        {
            _model.EnterTargetingMode();
            return;
        }

        if (ev.ElementId == View.SaveButton.Id)
        {
            ApplyChanges(true);
            return;
        }

        if (ev.ElementId == View.DiscardButton.Id)
        {
            Close();
            return;
        }

        if (!_model.HasSelected) return;

        if (ev.ElementId == View.IconPlus1.Id)   TryAdjustIcon(+1);
        else if (ev.ElementId == View.IconMinus1.Id)  TryAdjustIcon(-1);
        else if (ev.ElementId == View.IconPlus10.Id)  TryAdjustIcon(+10);
        else if (ev.ElementId == View.IconMinus10.Id) TryAdjustIcon(-10);
    }

    private void HandleWatch(string? elementId)
    {
        if (_initializing) return;
        if (!_model.HasSelected)
        {
            UpdateFromModel();
            return;
        }

        // We only watch Name and Description, so any watch event means text changed.
        ApplyChanges(false);
    }

    private void ApplyChanges(bool showMessage)
    {
        if (!_model.HasSelected) return;

        string? name = Token().GetBindValue(View.Name);
        string? desc = Token().GetBindValue(View.Description);
        if (name is null || desc is null) return;

        _model.UpdateBasic(name, desc);

        if (showMessage)
            _player.SendServerMessage("Item updated.", ColorConstants.Green);
    }

    private void TryAdjustIcon(int delta)
    {
        var result = _model.TryAdjustIcon(delta, out int newValue, out int maxValue);
        switch (result)
        {
            case IconAdjustResult.NotAllowedType:
                _player.SendServerMessage("This item type can't change icons in this tool.", ColorConstants.Orange);
                break;
            case IconAdjustResult.Success:
                Token().SetBindValue(View.IconInfo, $"Icon: {newValue} / {maxValue}");
                _player.SendServerMessage("Icon updated.", ColorConstants.Green);
                break;
            case IconAdjustResult.NoSelection:
                break;
        }
    }

    private void OnNewSelection()
    {
        UpdateFromModel();
    }

    private void UpdateFromModel()
    {
        _initializing = true;
        try
        {
            Token().SetBindValue(View.ValidObjectSelected, _model.HasSelected);

            if (!_model.HasSelected)
            {
                Token().SetBindValue(View.IconControlsVisible, false);
                Token().SetBindValue(View.IconInfo, "Icon: —");
                return;
            }

            Token().SetBindValue(View.Name, _model.Selected!.Name);
            Token().SetBindValue(View.Description, _model.Selected!.Description);

            // Icon controls: only visible when allowed by base type
            bool iconAllowed = _model.IsIconAllowed(out int current, out int max);
            Token().SetBindValue(View.IconControlsVisible, iconAllowed);
            Token().SetBindValue(View.IconInfo, iconAllowed ? $"Icon: {current} / {max}" : "Icon: —");
        }
        finally { _initializing = false; }
    }

    public override void Close()
    {
        Token().Close();
    }
}
