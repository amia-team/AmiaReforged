using AmiaReforged.PwEngine.Database.Entities.Admin;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.DreamcoinRentals;

/// <summary>
/// Presenter for the Dreamcoin Rentals DM tool window.
/// </summary>
public sealed class DreamcoinRentalPresenter : ScryPresenter<DreamcoinRentalView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public override DreamcoinRentalView View { get; }

    private readonly NwPlayer _dmPlayer;
    private readonly DreamcoinRentalModel _model;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    // For edit modal
    private NuiWindowToken? _editModalToken;
    private int? _editingRentalId;

    public override NuiWindowToken Token() => _token;

    public DreamcoinRentalPresenter(DreamcoinRentalView view, NwPlayer dmPlayer)
    {
        View = view;
        _dmPlayer = dmPlayer;
        _model = new DreamcoinRentalModel(dmPlayer);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(_model);

        _model.OnRentalsUpdated += OnRentalsUpdated;
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(100f, 100f, View.GetWindowWidth(), View.GetWindowHeight()),
            Resizable = true
        };
    }

    public override void Create()
    {
        if (_window is null) InitBefore();

        if (_window is null)
        {
            _dmPlayer.SendServerMessage("The window could not be created. Screenshot this message and report it to a DM.", ColorConstants.Orange);
            return;
        }

        if (!_dmPlayer.TryCreateNuiWindow(_window, out _token))
            return;

        // Initialize binds
        Token().SetBindValue(View.NewCdKey, "");
        Token().SetBindValue(View.NewMonthlyCost, "");
        Token().SetBindValue(View.NewDescription, "");
        Token().SetBindValue(View.SearchTerm, "");
        Token().SetBindValue(View.ShowInactive, false);

        // Watch for checkbox changes
        Token().SetBindWatch(View.ShowInactive, true);

        // Load initial data
        LoadRentalsAsync();
    }

    private async void LoadRentalsAsync()
    {
        bool showInactive = Token().GetBindValue(View.ShowInactive);
        _model.SetShowInactive(showInactive);

        await _model.LoadRentalsAsync();
        await NwTask.SwitchToMainThread();

        RefreshRentalList();
    }

    private void OnRentalsUpdated(DreamcoinRentalModel sender, EventArgs e)
    {
        NwTask.Run(async () =>
        {
            await NwTask.SwitchToMainThread();
            RefreshRentalList();
        });
    }

    private void RefreshRentalList()
    {
        List<string> cdKeys = [];
        List<string> costs = [];
        List<string> descriptions = [];
        List<string> statuses = [];
        List<Color> statusColors = [];

        foreach (DreamcoinRental rental in _model.VisibleRentals)
        {
            cdKeys.Add(rental.PlayerCdKey);
            costs.Add($"{rental.MonthlyCost} DC");
            descriptions.Add(rental.Description ?? "(No description)");

            if (!rental.IsActive)
            {
                statuses.Add("Inactive");
                statusColors.Add(new Color(128, 128, 128)); // Gray
            }
            else if (rental.IsDelinquent)
            {
                statuses.Add("Delinquent");
                statusColors.Add(new Color(255, 80, 80)); // Red
            }
            else
            {
                statuses.Add("Active");
                statusColors.Add(new Color(80, 255, 80)); // Green
            }
        }

        Token().SetBindValues(View.RentalCdKeys, cdKeys);
        Token().SetBindValues(View.RentalCosts, costs);
        Token().SetBindValues(View.RentalDescriptions, descriptions);
        Token().SetBindValues(View.RentalStatuses, statuses);
        Token().SetBindValues(View.RentalStatusColors, statusColors);
        Token().SetBindValue(View.RentalCount, _model.VisibleRentals.Count);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        // Handle checkbox watch events
        if (ev.EventType == NuiEventType.Watch && ev.ElementId == View.ShowInactive.Key)
        {
            LoadRentalsAsync();
            return;
        }

        if (ev.EventType != NuiEventType.Click)
            return;

        switch (ev.ElementId)
        {
            case DreamcoinRentalView.AddRentalButtonId:
                HandleAddRental();
                break;

            case DreamcoinRentalView.SearchButtonId:
                HandleSearch();
                break;

            case DreamcoinRentalView.EditRentalButtonId:
                HandleEditRental(ev.ArrayIndex);
                break;

            case DreamcoinRentalView.DeactivateRentalButtonId:
                HandleDeactivateRental(ev.ArrayIndex);
                break;

            case DreamcoinRentalView.ReactivateRentalButtonId:
                HandleReactivateRental(ev.ArrayIndex);
                break;

            case DreamcoinRentalView.DeleteRentalButtonId:
                HandleDeleteRental(ev.ArrayIndex);
                break;

            case DreamcoinRentalView.ClearDelinquentButtonId:
                HandleClearDelinquent(ev.ArrayIndex);
                break;

            case "btn_confirm_edit":
                HandleConfirmEdit();
                break;

            case "btn_cancel_edit":
                _editModalToken?.Close();
                _editModalToken = null;
                _editingRentalId = null;
                break;
        }
    }

    private async void HandleAddRental()
    {
        string cdKey = Token().GetBindValue(View.NewCdKey) ?? "";
        string costStr = Token().GetBindValue(View.NewMonthlyCost) ?? "0";
        string description = Token().GetBindValue(View.NewDescription) ?? "";

        if (!int.TryParse(costStr, out int monthlyCost))
        {
            _dmPlayer.SendServerMessage("Please enter a valid number for monthly cost.", ColorConstants.Red);
            return;
        }

        bool success = await _model.AddRentalAsync(cdKey, monthlyCost, description);
        await NwTask.SwitchToMainThread();

        if (success)
        {
            // Clear inputs after successful add
            Token().SetBindValue(View.NewCdKey, "");
            Token().SetBindValue(View.NewMonthlyCost, "");
            Token().SetBindValue(View.NewDescription, "");
        }
    }

    private void HandleSearch()
    {
        string searchTerm = Token().GetBindValue(View.SearchTerm) ?? "";
        _model.SetSearchTerm(searchTerm);
        RefreshRentalList();
    }

    private void HandleEditRental(int arrayIndex)
    {
        if (arrayIndex < 0 || arrayIndex >= _model.VisibleRentals.Count)
            return;

        DreamcoinRental rental = _model.VisibleRentals[arrayIndex];
        _editingRentalId = rental.Id;

        ShowEditModal(rental);
    }

    private void ShowEditModal(DreamcoinRental rental)
    {
        NuiBind<string> editCost = new("edit_cost");
        NuiBind<string> editDescription = new("edit_description");

        NuiColumn modalLayout = new()
        {
            Children =
            [
                new NuiRow
                {
                    Height = 30f,
                    Children =
                    [
                        new NuiLabel($"Editing rental for: {rental.PlayerCdKey}")
                        {
                            VerticalAlign = NuiVAlign.Middle
                        }
                    ]
                },
                new NuiRow
                {
                    Height = 35f,
                    Children =
                    [
                        new NuiLabel("Monthly DC:") { Width = 100f, VerticalAlign = NuiVAlign.Middle },
                        new NuiTextEdit("Cost", editCost, 10, false) { Width = 100f }
                    ]
                },
                new NuiRow
                {
                    Height = 35f,
                    Children =
                    [
                        new NuiLabel("Description:") { Width = 100f, VerticalAlign = NuiVAlign.Middle },
                        new NuiTextEdit("Note", editDescription, 500, false) { Width = 250f }
                    ]
                },
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiButton("Save") { Id = "btn_confirm_edit", Width = 80f },
                        new NuiSpacer { Width = 20f },
                        new NuiButton("Cancel") { Id = "btn_cancel_edit", Width = 80f }
                    ]
                }
            ]
        };

        NuiWindow editWindow = new(modalLayout, "Edit Rental")
        {
            Geometry = new NuiRect(200f, 200f, 400f, 200f)
        };

        if (_dmPlayer.TryCreateNuiWindow(editWindow, out NuiWindowToken editToken))
        {
            _editModalToken = editToken;
            editToken.SetBindValue(editCost, rental.MonthlyCost.ToString());
            editToken.SetBindValue(editDescription, rental.Description ?? "");
        }
    }

    private async void HandleConfirmEdit()
    {
        if (_editModalToken is null || _editingRentalId is null)
            return;

        NuiBind<string> editCost = new("edit_cost");
        NuiBind<string> editDescription = new("edit_description");

        string costStr = _editModalToken.Value.GetBindValue(editCost) ?? "0";
        string description = _editModalToken.Value.GetBindValue(editDescription) ?? "";

        if (!int.TryParse(costStr, out int monthlyCost))
        {
            _dmPlayer.SendServerMessage("Please enter a valid number for monthly cost.", ColorConstants.Red);
            return;
        }

        await _model.UpdateRentalAsync(_editingRentalId.Value, monthlyCost, description);
        await NwTask.SwitchToMainThread();

        _editModalToken?.Close();
        _editModalToken = null;
        _editingRentalId = null;
    }

    private async void HandleDeactivateRental(int arrayIndex)
    {
        if (arrayIndex < 0 || arrayIndex >= _model.VisibleRentals.Count)
            return;

        DreamcoinRental rental = _model.VisibleRentals[arrayIndex];
        await _model.DeactivateRentalAsync(rental.Id);
    }

    private async void HandleReactivateRental(int arrayIndex)
    {
        if (arrayIndex < 0 || arrayIndex >= _model.VisibleRentals.Count)
            return;

        DreamcoinRental rental = _model.VisibleRentals[arrayIndex];
        await _model.ReactivateRentalAsync(rental.Id);
    }

    private async void HandleDeleteRental(int arrayIndex)
    {
        if (arrayIndex < 0 || arrayIndex >= _model.VisibleRentals.Count)
            return;

        DreamcoinRental rental = _model.VisibleRentals[arrayIndex];

        // Show confirmation message
        _dmPlayer.SendServerMessage($"Deleting rental for {rental.PlayerCdKey}...", ColorConstants.Orange);

        await _model.DeleteRentalAsync(rental.Id);
    }

    private async void HandleClearDelinquent(int arrayIndex)
    {
        if (arrayIndex < 0 || arrayIndex >= _model.VisibleRentals.Count)
            return;

        DreamcoinRental rental = _model.VisibleRentals[arrayIndex];
        await _model.ClearDelinquencyAsync(rental.Id);
    }

    public override void Close()
    {
        _editModalToken?.Close();
        _editModalToken = null;
        _editingRentalId = null;
    }
}
