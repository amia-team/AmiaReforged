using AmiaReforged.PwEngine.Features.Player.PlayerTools.Services;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.ItemTool;

public sealed class ItemToolPresenter : ScryPresenter<ItemToolView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private ItemToolModel? _model;

    private NuiWindowToken? _nameModalToken;
    private NuiWindowToken? _descModalToken;

    [Inject] private Lazy<IRenameItemService> RenameService { get; init; } = null!;

    public ItemToolPresenter(ItemToolView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override ItemToolView View { get; }
    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        // Initialize model after DI
        _model = new ItemToolModel(_player, RenameService.Value);
        _model.OnNewSelection += OnNewSelection;

        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(0f, 100f, 635f, 550f),
            Resizable = true
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

        Token().SetBindValue(View.ValidObjectSelected, false);
        Token().SetBindValue(View.Name, "");
        Token().SetBindValue(View.Description, "");
        Token().SetBindValue(View.DescPlaceholder, "");

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
        if (_model == null) return;

        if (ev.ElementId == View.SelectItemButton.Id)
        {
            _model.EnterTargetingMode();
            return;
        }

        if (ev.ElementId == View.SaveButton.Id)
        {
            ApplyChanges(true);
            _model.ClearInitials();
            _descModalToken?.Close();
            _nameModalToken?.Close();
            return;
        }

        if (ev.ElementId == View.CancelButton.Id)
        {
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
            _model.EnsureInitialNameCaptured();
            Token().SetBindValue(View.EditNameBuffer, _model.GetInitialNameOrCurrent());

            NuiWindow w = View.BuildEditNameModal();
            if (_player.TryCreateNuiWindow(w, out NuiWindowToken modalToken))
            {
                _nameModalToken = modalToken;
                modalToken.SetBindValue(View.EditNameBuffer, _model.Selected!.Name);
                _nameModalToken.Value.OnNuiEvent += HandleNameModalEvent;
            }
            return;
        }

        if (ev.ElementId == "ind_edit_desc")
        {
            _model.EnsureInitialDescCaptured();
            Token().SetBindValue(View.EditDescBuffer, _model.GetInitialDescOrCurrent());

            NuiWindow w = View.BuildEditDescModal();
            if (_player.TryCreateNuiWindow(w, out NuiWindowToken modalToken))
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
            string newName = _nameModalToken!.Value.GetBindValue(View.EditNameBuffer) ?? string.Empty;
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
            string newDesc = _descModalToken!.Value.GetBindValue(View.EditDescBuffer) ?? string.Empty;
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
        if (_model == null || !_model.HasSelected) return;

        string? name = Token().GetBindValue(View.Name);
        string? desc = Token().GetBindValue(View.Description);

        _model.UpdateBasic(name ?? string.Empty, desc ?? string.Empty);

        if (showMessage)
            _player.SendServerMessage("Item updated.", ColorConstants.Green);
    }

    private void TryAdjustIcon(int delta)
    {
        if (_model == null) return;

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
        if (_model == null) return;

        _model.EnsureInitialNameCaptured();
        _model.EnsureInitialDescCaptured();
        UpdateFromModel();
        _descModalToken?.Close();
        _nameModalToken?.Close();
    }

    private void UpdateFromModel()
    {
        NwItem? item = _model.Selected;
        Token().SetBindValue(View.ValidObjectSelected, item != null);
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
