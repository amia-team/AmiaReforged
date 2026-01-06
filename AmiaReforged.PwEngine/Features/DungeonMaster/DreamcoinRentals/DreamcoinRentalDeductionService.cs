using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Database.Entities.Admin;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.DreamcoinRentals;

/// <summary>
/// Service that processes monthly dreamcoin rental deductions.
/// Runs on the 1st of each month (or catches up if server was down).
/// Marks rentals as delinquent if the player doesn't have enough dreamcoins.
/// </summary>
[ServiceBinding(typeof(DreamcoinRentalDeductionService))]
public sealed class DreamcoinRentalDeductionService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const int CheckIntervalMinutes = 60; // Check once per hour

    private readonly DreamcoinService _dreamcoinService;
    private readonly IDreamcoinRentalRepository _rentalRepository;
    private readonly SchedulerService _schedulerService;
    private readonly bool _isLiveServer;

    private DateTime _lastProcessedDate = DateTime.MinValue;

    public DreamcoinRentalDeductionService(
        DreamcoinService dreamcoinService,
        IDreamcoinRentalRepository rentalRepository,
        SchedulerService schedulerService)
    {
        _dreamcoinService = dreamcoinService;
        _rentalRepository = rentalRepository;
        _schedulerService = schedulerService;

        // Check if we're on live server
        string environment = UtilPlugin.GetEnvironmentVariable("SERVER_MODE");
        _isLiveServer = environment == "live";

        if (!_isLiveServer)
        {
            Log.Info("DreamcoinRentalDeductionService: Not on live server, rental deductions disabled.");
            return;
        }

        // Schedule the deduction check to run periodically
        _schedulerService.ScheduleRepeating(OnDeductionCheck, TimeSpan.FromMinutes(CheckIntervalMinutes));

        // Also run once at startup to catch up on any missed deductions
        NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromSeconds(30)); // Wait for server to be fully up
            await ProcessDueRentalsAsync();
        });

        Log.Info("DreamcoinRentalDeductionService initialized. Monthly rental deductions enabled.");
    }

    private async void OnDeductionCheck()
    {
        await ProcessDueRentalsAsync();
    }

    /// <summary>
    /// Processes all rentals that are due for payment.
    /// </summary>
    private async Task ProcessDueRentalsAsync()
    {
        try
        {
            DateTime now = DateTime.UtcNow;

            // Get all rentals that are due
            List<DreamcoinRental> dueRentals = await _rentalRepository.GetRentalsDueForPaymentAsync(now);

            if (dueRentals.Count == 0)
            {
                return;
            }

            Log.Info($"Processing {dueRentals.Count} rentals due for payment.");

            foreach (DreamcoinRental rental in dueRentals)
            {
                await ProcessRentalPaymentAsync(rental);
            }

            await NwTask.SwitchToMainThread();
            Log.Info($"Completed processing {dueRentals.Count} rental payments.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing rental deductions");
        }
    }

    /// <summary>
    /// Processes a single rental payment.
    /// </summary>
    private async Task ProcessRentalPaymentAsync(DreamcoinRental rental)
    {
        try
        {
            // Get current balance
            int currentBalance = await _dreamcoinService.GetDreamcoins(rental.PlayerCdKey);

            if (currentBalance >= rental.MonthlyCost)
            {
                // Player has enough - deduct the cost
                int newBalance = await _dreamcoinService.RemoveDreamcoins(rental.PlayerCdKey, rental.MonthlyCost);

                if (newBalance >= 0)
                {
                    // Mark as paid and set next due date
                    await _rentalRepository.MarkPaidAsync(rental.Id, DateTime.UtcNow);

                    Log.Info($"Rental {rental.Id}: Deducted {rental.MonthlyCost} DC from {rental.PlayerCdKey}. " +
                             $"New balance: {newBalance}. Description: {rental.Description ?? "N/A"}");

                    // Notify player if online
                    await NwTask.SwitchToMainThread();
                    NotifyPlayerIfOnline(rental.PlayerCdKey,
                        $"Monthly rental payment of {rental.MonthlyCost} DC has been processed. " +
                        $"({rental.Description ?? "Rental"})");
                }
                else
                {
                    // Failed to deduct - mark as delinquent
                    await _rentalRepository.MarkDelinquentAsync(rental.Id);
                    Log.Warn($"Rental {rental.Id}: Failed to deduct DC for {rental.PlayerCdKey}. Marked delinquent.");
                }
            }
            else
            {
                // Insufficient funds - mark as delinquent
                await _rentalRepository.MarkDelinquentAsync(rental.Id);

                Log.Info($"Rental {rental.Id}: Insufficient funds for {rental.PlayerCdKey}. " +
                         $"Required: {rental.MonthlyCost}, Available: {currentBalance}. Marked delinquent.");

                // Notify player if online
                await NwTask.SwitchToMainThread();
                NotifyPlayerIfOnline(rental.PlayerCdKey,
                    $"Insufficient Dreamcoins for rental payment of {rental.MonthlyCost} DC. " +
                    $"Your rental is now delinquent. ({rental.Description ?? "Rental"})");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error processing rental payment for rental {rental.Id}");
        }
    }

    /// <summary>
    /// Notifies a player if they are currently online.
    /// </summary>
    private void NotifyPlayerIfOnline(string cdKey, string message)
    {
        NwPlayer? player = NwModule.Instance.Players
            .FirstOrDefault(p => p.IsValid && p.CDKey == cdKey);

        player?.SendServerMessage(message, ColorConstants.Yellow);
    }

    /// <summary>
    /// Forces an immediate processing of all due rentals. Useful for testing or manual trigger.
    /// </summary>
    public async Task ForceProcessDueRentalsAsync()
    {
        await ProcessDueRentalsAsync();
    }
}
