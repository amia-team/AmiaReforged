using AmiaReforged.PwEngine.Database.Entities.Admin;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.DreamcoinRentals;

/// <summary>
/// Model for managing dreamcoin rentals in the DM tool window.
/// </summary>
public sealed class DreamcoinRentalModel
{
    [Inject] private Lazy<IDreamcoinRentalRepository> Repository { get; init; } = null!;

    private readonly NwPlayer _dmPlayer;
    private string _searchTerm = string.Empty;
    private bool _showInactiveRentals;

    public List<DreamcoinRental> AllRentals { get; private set; } = [];
    public List<DreamcoinRental> VisibleRentals { get; private set; } = [];
    public DreamcoinRental? SelectedRental { get; set; }

    public delegate void RentalUpdateEventHandler(DreamcoinRentalModel sender, EventArgs e);
    public event RentalUpdateEventHandler? OnRentalsUpdated;

    public DreamcoinRentalModel(NwPlayer dmPlayer)
    {
        _dmPlayer = dmPlayer;
    }

    public async Task LoadRentalsAsync()
    {
        AllRentals = _showInactiveRentals
            ? await Repository.Value.GetAllAsync()
            : await Repository.Value.GetAllActiveAsync();

        RefreshVisibleRentals();
    }

    public void SetSearchTerm(string search)
    {
        _searchTerm = search;
        RefreshVisibleRentals();
    }

    public void SetShowInactive(bool showInactive)
    {
        _showInactiveRentals = showInactive;
    }

    public void RefreshVisibleRentals()
    {
        if (string.IsNullOrWhiteSpace(_searchTerm))
        {
            VisibleRentals = AllRentals.ToList();
        }
        else
        {
            VisibleRentals = AllRentals
                .Where(r =>
                    r.PlayerCdKey.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (r.Description?.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        OnRentalsUpdated?.Invoke(this, EventArgs.Empty);
    }

    public async Task<bool> AddRentalAsync(string playerCdKey, int monthlyCost, string? description)
    {
        if (string.IsNullOrWhiteSpace(playerCdKey))
        {
            _dmPlayer.SendServerMessage("CD Key is required.", ColorConstants.Red);
            return false;
        }

        if (monthlyCost <= 0)
        {
            _dmPlayer.SendServerMessage("Monthly cost must be greater than 0.", ColorConstants.Red);
            return false;
        }

        // Calculate next due date (1st of next month)
        DateTime now = DateTime.UtcNow;
        DateTime nextDueDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

        DreamcoinRental rental = new()
        {
            PlayerCdKey = playerCdKey.Trim(),
            MonthlyCost = monthlyCost,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedUtc = now,
            CreatedByDmCdKey = _dmPlayer.CDKey,
            IsActive = true,
            IsDelinquent = false,
            NextDueDateUtc = nextDueDate
        };

        try
        {
            await Repository.Value.AddAsync(rental);
            await NwTask.SwitchToMainThread();
            _dmPlayer.SendServerMessage($"Rental created for {playerCdKey}. Due: {monthlyCost} DC on the 1st of each month.", ColorConstants.Lime);
            await LoadRentalsAsync();
            return true;
        }
        catch (Exception ex)
        {
            await NwTask.SwitchToMainThread();
            _dmPlayer.SendServerMessage($"Failed to create rental: {ex.Message}", ColorConstants.Red);
            return false;
        }
    }

    public async Task<bool> UpdateRentalAsync(int rentalId, int monthlyCost, string? description)
    {
        if (monthlyCost <= 0)
        {
            _dmPlayer.SendServerMessage("Monthly cost must be greater than 0.", ColorConstants.Red);
            return false;
        }

        try
        {
            DreamcoinRental? rental = await Repository.Value.GetByIdAsync(rentalId);
            await NwTask.SwitchToMainThread();
            if (rental == null)
            {
                _dmPlayer.SendServerMessage("Rental not found.", ColorConstants.Red);
                return false;
            }

            rental.MonthlyCost = monthlyCost;
            rental.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

            await Repository.Value.UpdateAsync(rental);
            await NwTask.SwitchToMainThread();
            _dmPlayer.SendServerMessage($"Rental updated successfully.", ColorConstants.Lime);
            await LoadRentalsAsync();
            return true;
        }
        catch (Exception ex)
        {
            await NwTask.SwitchToMainThread();
            _dmPlayer.SendServerMessage($"Failed to update rental: {ex.Message}", ColorConstants.Red);
            return false;
        }
    }

    public async Task<bool> DeactivateRentalAsync(int rentalId)
    {
        try
        {
            await Repository.Value.DeactivateAsync(rentalId);
            await NwTask.SwitchToMainThread();
            _dmPlayer.SendServerMessage("Rental deactivated.", ColorConstants.Lime);
            await LoadRentalsAsync();
            return true;
        }
        catch (Exception ex)
        {
            await NwTask.SwitchToMainThread();
            _dmPlayer.SendServerMessage($"Failed to deactivate rental: {ex.Message}", ColorConstants.Red);
            return false;
        }
    }

    public async Task<bool> DeleteRentalAsync(int rentalId)
    {
        try
        {
            await Repository.Value.DeleteAsync(rentalId);
            await NwTask.SwitchToMainThread();
            _dmPlayer.SendServerMessage("Rental deleted permanently.", ColorConstants.Lime);
            await LoadRentalsAsync();
            return true;
        }
        catch (Exception ex)
        {
            await NwTask.SwitchToMainThread();
            _dmPlayer.SendServerMessage($"Failed to delete rental: {ex.Message}", ColorConstants.Red);
            return false;
        }
    }

    public async Task<bool> ClearDelinquencyAsync(int rentalId)
    {
        try
        {
            DreamcoinRental? rental = await Repository.Value.GetByIdAsync(rentalId);
            await NwTask.SwitchToMainThread();
            if (rental == null)
            {
                _dmPlayer.SendServerMessage("Rental not found.", ColorConstants.Red);
                return false;
            }

            await Repository.Value.MarkPaidAsync(rentalId, DateTime.UtcNow);
            await NwTask.SwitchToMainThread();
            _dmPlayer.SendServerMessage("Delinquency cleared and next due date updated.", ColorConstants.Lime);
            await LoadRentalsAsync();
            return true;
        }
        catch (Exception ex)
        {
            await NwTask.SwitchToMainThread();
            _dmPlayer.SendServerMessage($"Failed to clear delinquency: {ex.Message}", ColorConstants.Red);
            return false;
        }
    }
}
