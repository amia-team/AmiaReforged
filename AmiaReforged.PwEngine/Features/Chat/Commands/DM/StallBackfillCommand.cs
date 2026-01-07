using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.DM;

/// <summary>
/// DM command to backfill missing BaseItemType values for player stall products.
/// Usage: ./stallbackfill
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public sealed class StallBackfillCommand : IChatCommand
{
    private readonly StallProductBackfillService _backfillService;

    public StallBackfillCommand(StallProductBackfillService backfillService)
    {
        _backfillService = backfillService ?? throw new ArgumentNullException(nameof(backfillService));
    }

    public string Command => "./stallbackfill";
    public string Description => "Backfill missing item types for stall products";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM)
        {
            caller.SendServerMessage("This command is only available to DMs.", ColorConstants.Red);
            return;
        }

        if (_backfillService.IsRunning)
        {
            caller.SendServerMessage("A backfill operation is already in progress. Please wait.", ColorConstants.Orange);
            return;
        }

        caller.SendServerMessage("Starting stall product item type backfill...", ColorConstants.Lime);
        caller.SendServerMessage("This may take a moment. Items will be temporarily created and destroyed.", ColorConstants.White);

        BackfillResult result = await _backfillService.RunBackfillAsync();

        if (result.Success)
        {
            caller.SendServerMessage($"Backfill complete!", ColorConstants.Lime);
            caller.SendServerMessage($"  Processed: {result.Processed}", ColorConstants.White);
            caller.SendServerMessage($"  Updated: {result.Updated}", ColorConstants.White);
            caller.SendServerMessage($"  Failed: {result.Failed}", ColorConstants.White);
        }
        else
        {
            caller.SendServerMessage($"Backfill failed: {result.Message}", ColorConstants.Red);
        }
    }
}
