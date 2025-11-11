using AmiaReforged.PwEngine.Features.WorldEngine;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.DM;

/// <summary>
/// DM command to manually foreclose a property. Useful for inactive players or administrative actions.
/// Usage: ./foreclose {numeric_settlement_id} {poi_resref}
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class ForeclosePropertyCommand(
    RegionIndex regionIndex,
    IRentablePropertyRepository propertyRepository,
    IWorldEngineFacade worldEngine) : IChatCommand
{
    public string Command => "./foreclose";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (caller is { IsDM: false, IsPlayerDM: false })
        {
            caller.SendServerMessage("This command is only available to DMs.", ColorConstants.Red);
            return;
        }

        if (args.Length < 2)
        {
            caller.SendServerMessage("Usage: ./foreclose <numeric_settlement_id> <poi_resref>", ColorConstants.Orange);
            caller.SendServerMessage("Example: ./foreclose 1 mycave_01", ColorConstants.Gray);
            return;
        }

        // Parse settlement ID
        if (!int.TryParse(args[0], out int settlementIdValue))
        {
            caller.SendServerMessage($"Invalid settlement ID: '{args[0]}'. Must be a numeric value.", ColorConstants.Red);
            return;
        }

        string poiResRef = args[1];
        SettlementId settlementId = SettlementId.Parse(settlementIdValue);

        caller.SendServerMessage($"Attempting to foreclose property: Settlement {settlementIdValue}, POI '{poiResRef}'...", ColorConstants.Yellow);

        // Step 1: Validate the settlement exists and has the POI
        IReadOnlyList<PlaceOfInterest> pois = regionIndex.GetPointsOfInterestForSettlement(settlementId);
        PlaceOfInterest? targetPoi = pois.FirstOrDefault(p =>
            string.Equals(p.ResRef, poiResRef, StringComparison.OrdinalIgnoreCase));

        if (targetPoi is null)
        {
            caller.SendServerMessage($"❌ POI '{poiResRef}' not found in settlement {settlementIdValue}.", ColorConstants.Red);
            caller.SendServerMessage($"Available POIs in settlement {settlementIdValue}:", ColorConstants.Gray);
            foreach (PlaceOfInterest poi in pois.Take(10))
            {
                caller.SendServerMessage($"  • {poi.ResRef} ({poi.Name})", ColorConstants.Gray);
            }
            if (pois.Count > 10)
            {
                caller.SendServerMessage($"  ... and {pois.Count - 10} more", ColorConstants.Gray);
            }
            return;
        }

        // Step 2: Look up the property by internal name (POI Name)
        RentablePropertySnapshot? property = await propertyRepository.GetSnapshotByInternalNameAsync(
            targetPoi.Name,
            CancellationToken.None).ConfigureAwait(false);

        // Return to main thread before making NWN API calls
        await NwTask.SwitchToMainThread();

        if (property is null)
        {
            caller.SendServerMessage($"❌ No rentable property found for POI '{poiResRef}' (Internal Name: '{targetPoi.Name}').", ColorConstants.Red);
            caller.SendServerMessage("This POI may not be configured as a rentable property.", ColorConstants.Gray);
            return;
        }

        // Step 3: Check if property is actually occupied
        if (property.CurrentTenant is null && property.CurrentOwner is null)
        {
            caller.SendServerMessage($"⚠️ Property '{targetPoi.Name}' is not currently occupied.", ColorConstants.Orange);
            caller.SendServerMessage("No foreclosure action needed - property is already vacant.", ColorConstants.Gray);
            return;
        }

        // Step 4: Execute the eviction
        caller.SendServerMessage($"🏠 Foreclosing property '{targetPoi.Name}' (POI: {poiResRef})", ColorConstants.Cyan);
        if (property.CurrentTenant is not null)
        {
            caller.SendServerMessage($"   Current Tenant: {property.CurrentTenant}", ColorConstants.Gray);
        }
        if (property.CurrentOwner is not null)
        {
            caller.SendServerMessage($"   Current Owner: {property.CurrentOwner}", ColorConstants.Gray);
        }

        try
        {
            CommandResult result = await worldEngine.ExecuteAsync(
                new EvictPropertyCommand(property),
                CancellationToken.None).ConfigureAwait(false);

            // Return to main thread before making NWN API calls
            await NwTask.SwitchToMainThread();

            if (result.Success)
            {
                caller.SendServerMessage($"✓ Property foreclosed successfully!", ColorConstants.Green);
                caller.SendServerMessage($"  • Placeables deleted or moved to foreclosure storage", ColorConstants.Gray);
                caller.SendServerMessage($"  • Property state cleared", ColorConstants.Gray);
                caller.SendServerMessage($"  • Property is now available for rent", ColorConstants.Gray);
            }
            else
            {
                caller.SendServerMessage($"❌ Foreclosure failed: {result.ErrorMessage ?? "Unknown error"}", ColorConstants.Red);
            }
        }
        catch (Exception ex)
        {
            caller.SendServerMessage($"❌ Exception during foreclosure: {ex.Message}", ColorConstants.Red);
        }
    }
}
