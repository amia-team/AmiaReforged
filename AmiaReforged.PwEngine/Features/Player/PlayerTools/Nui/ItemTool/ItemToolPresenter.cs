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

    // Modal tokens so we can close them programmatically
    private NuiWindowToken? _nameModalToken;
    private NuiWindowToken? _descModalToken;

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
            Geometry = new NuiRect(520f, 140f, 615f, 500f),
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

        // Initial bind state
        Token().SetBindValue(View.ValidObjectSelected, false);
        Token().SetBindValue(View.Name, "");
        Token().SetBindValue(View.Description, "");
        Token().SetBindValue(View.DescPlaceholder, "");

        // Don't watch main window binds - we don't do live updates in main window
        Token().SetBindWatch(View.Name, false);
        Token().SetBindWatch(View.Description, false);

        UpdateFromModel();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click)
            return;

        HandleClick(ev);
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
            // Save and clear any initial value locals
            ApplyChanges(true);
            _model.ClearInitials();
            _descModalToken?.Close();
            _nameModalToken?.Close();
            return;
        }

        if (ev.ElementId == View.CancelButton.Id)
        {
            // Revert if any and clear, then close
            _model.RevertNameToInitial();
            _model.RevertDescToInitial();
            _model.ClearInitials();
            Close();
            return;
        }

        if (!_model.HasSelected) return;

        if (ev.ElementId == View.IconPlus1.Id)   { TryAdjustIcon(+1); return; }
        if (ev.ElementId == View.IconMinus1.Id)  { TryAdjustIcon(-1); return; }
        if (ev.ElementId == View.IconPlus10.Id)  { TryAdjustIcon(+10); return; }
        if (ev.ElementId == View.IconMinus10.Id) { TryAdjustIcon(-10); return; }

        if (ev.ElementId == "ind_edit_name")
        {
            // Capture initial name and set buffer BEFORE creating modal
            _model.EnsureInitialNameCaptured();
            Token().SetBindValue(View.EditNameBuffer, _model.GetInitialNameOrCurrent());

            var w = View.BuildEditNameModal();
            if (_player.TryCreateNuiWindow(w, out var modalToken))
            {
                _nameModalToken = modalToken;
                modalToken.SetBindValue(View.EditNameBuffer, _model.Selected!.Name);
                _nameModalToken.Value.OnNuiEvent += HandleNameModalEvent;
            }
            return;
        }

        if (ev.ElementId == "ind_edit_desc")
        {
            // Capture initial desc and set buffer BEFORE creating modal
            _model.EnsureInitialDescCaptured();
            Token().SetBindValue(View.EditDescBuffer, _model.GetInitialDescOrCurrent());

            var w = View.BuildEditDescModal();
            if (_player.TryCreateNuiWindow(w, out var modalToken))
            {
                _descModalToken = modalToken;
                modalToken.SetBindValue(View.EditDescBuffer, _model.Selected!.Description);
                _descModalToken.Value.OnNuiEvent += HandleDescModalEvent;
            }
        }
    }

    private void HandleNameModalEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click)
            return;

        if (ev.ElementId == "ind_modal_ok_name")
        {
            var newName = _nameModalToken!.Value.GetBindValue(View.EditNameBuffer) ?? string.Empty;
            Token().SetBindValue(View.Name, newName);
            ApplyChanges(true);
            if (_nameModalToken.HasValue)
            {
                _nameModalToken.Value.OnNuiEvent -= HandleNameModalEvent;
                _nameModalToken?.Close();
                _nameModalToken = null;
            }
            return;
        }

        if (ev.ElementId == "ind_modal_discard_name")
        {
            _model.RevertNameToInitial();
            Token().SetBindValue(View.Name, _model.GetInitialNameOrCurrent());
            ApplyChanges(false);
            if (_nameModalToken.HasValue)
            {
                _nameModalToken.Value.OnNuiEvent -= HandleNameModalEvent;
                _nameModalToken?.Close();
                _nameModalToken = null;
            }
        }
    }

    private void HandleDescModalEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click)
            return;

        if (ev.ElementId == "ind_modal_ok_desc")
        {
            var newDesc = _descModalToken!.Value.GetBindValue(View.EditDescBuffer) ?? string.Empty;
            Token().SetBindValue(View.Description, newDesc);
            ApplyChanges(true);
            if (_descModalToken.HasValue)
            {
                _descModalToken.Value.OnNuiEvent -= HandleDescModalEvent;
                _descModalToken?.Close();
                _descModalToken = null;
            }
            return;
        }

        if (ev.ElementId == "ind_modal_discard_desc")
        {
            _model.RevertDescToInitial();
            Token().SetBindValue(View.Description, _model.GetInitialDescOrCurrent());
            ApplyChanges(false);
            if (_descModalToken.HasValue)
            {
                _descModalToken.Value.OnNuiEvent -= HandleDescModalEvent;
                _descModalToken?.Close();
                _descModalToken = null;
            }
        }
    }

    private void ApplyChanges(bool showMessage)
    {
        if (!_model.HasSelected) return;

        string? name = Token().GetBindValue(View.Name);
        string? desc = Token().GetBindValue(View.Description);

        _model.UpdateBasic(name ?? string.Empty, desc ?? string.Empty);

        if (showMessage)
            _player.SendServerMessage("Item updated.", ColorConstants.Green);
    }

    private void TryAdjustIcon(int delta)
    {
        IconAdjustResult result = _model.TryAdjustIcon(delta, out int newValue, out int maxValue);
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
            case IconAdjustResult.NoValidModel:
                _player.SendServerMessage("No valid icon found to switch to.", ColorConstants.Orange);
                break;
        }
    }

    private void OnNewSelection()
    {
        // Capture initial values immediately on selection so Discard/Cancel can always revert.
        _model.EnsureInitialNameCaptured();
        _model.EnsureInitialDescCaptured();
        UpdateFromModel();
        _descModalToken?.Close();
        _nameModalToken?.Close();
    }

    private void UpdateFromModel()
    {
        var item = _model.Selected;
        Token().SetBindValue(View.ValidObjectSelected, item != null);
        // Always show the same placeholder for Description in the main window
        Token().SetBindValue(View.DescPlaceholder, item != null ? "Edit to View" : "");

        Token().SetBindValue(View.ValidObjectSelected, _model.HasSelected);

        if (!_model.HasSelected)
        {
            Token().SetBindValue(View.IconControlsVisible, false);
            Token().SetBindValue(View.IconInfo, "Icon: —");
            Token().SetBindValue(View.Name, "");
            Token().SetBindValue(View.Description, "");
            return;
        }

        Token().SetBindValue(View.Name, _model.Selected!.Name);
        Token().SetBindValue(View.Description, _model.Selected!.Description);

        // Icon controls: only visible when allowed by base type
        bool iconAllowed = _model.IsIconAllowed(out int current, out int max);
        Token().SetBindValue(View.IconControlsVisible, iconAllowed);
        Token().SetBindValue(View.IconInfo, iconAllowed ? $"Icon: {current} / {max}" : "Icon: —");
    }

    public override void Close()
    {
        if (_nameModalToken.HasValue)
        {
            _nameModalToken.Value.OnNuiEvent -= HandleNameModalEvent;
            _nameModalToken?.Close();
            _nameModalToken = null;
        }

        if (_descModalToken.HasValue)
        {
            _descModalToken.Value.OnNuiEvent -= HandleDescModalEvent;
            _descModalToken?.Close();
            _descModalToken = null;
        }

        Token().Close();
    }
}
