using AmiaReforged.Core.Models.Sailing;
using AmiaReforged.Core.Services;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipStatePersistenceService))]
public class ShipStatePersistenceService
{
    private readonly DatabaseContextFactory
        _ctxFactory;

    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    private const bool PersistShips = false;
        
    public ShipStatePersistenceService(
        DatabaseContextFactory ctxFactory)
    {
        _ctxFactory = ctxFactory;
    }

    /// <summary>
    /// Loads the saved state for a ship.
    /// Returns null if the ship has never been saved.
    /// </summary>
    public async Task<SavedShipState?> LoadState(
        string shipName)
    {
        if (!PersistShips)
        {
        return null;
        }
        await using AmiaDbContext context =
            _ctxFactory.CreateDbContext();

        try
        {
            SavedShipState? state =
                await context.SavedShipStates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.ShipName == shipName);

            if (state != null)
            {
                Log.Info(
                    "Loaded saved ship state: " +
                    "Ship={ShipName}, Area={Area}, " +
                    "X={X}, Y={Y}, Z={Z}, " +
                    "Heading={Heading}, Underway={Underway}, " +
                    "Hull={Hull}, Weapon={Weapon}",
                    state.ShipName,
                    state.AreaResRef,
                    state.X,
                    state.Y,
                    state.Z,
                    state.Heading,
                    state.Underway,
                    state.Hull,
                    state.WeaponResRef);
            }
            else
            {
                Log.Info(
                    "No saved ship state found for '{ShipName}'.",
                    shipName);
            }

            return state;
        }
        catch (Exception e)
        {
            Log.Error(
                e,
                "Error loading saved ship state for '{ShipName}'.",
                shipName);

            return null;
        }
    }

    /// <summary>
    /// Saves the current state of a ship.
    /// Creates the database record if it does not exist.
    /// </summary>
    public async Task SaveState(
        ShipState shipState)
    {
        if (!PersistShips)
        {
        return;
        }
        await using AmiaDbContext context =
            _ctxFactory.CreateDbContext();

        try
        {
            SavedShipState? existingState =
                await context.SavedShipStates
                    .FirstOrDefaultAsync(
                        x =>
                            x.ShipName ==
                            shipState.ShipName);

            if (existingState == null)
            {
                existingState = new SavedShipState
                {
                    ShipName =
                        shipState.ShipName,

                    AreaResRef =
                        shipState.AreaResRef,

                    X =
                        shipState.X,

                    Y =
                        shipState.Y,

                    Z =
                        shipState.Z,

                    Heading =
                        shipState.Heading,

                    Underway =
                        shipState.Underway,

                    Hull =
                        shipState.Hull,

                    WeaponResRef =
                        string.IsNullOrWhiteSpace(
                            shipState.WeaponResRef)
                            ? "ship_cannon"
                            : shipState.WeaponResRef
                };

                await context.SavedShipStates
                    .AddAsync(existingState);

                Log.Info(
                    "Creating saved ship state for '{ShipName}'.",
                    shipState.ShipName);
            }
            else
            {
                existingState.AreaResRef =
                    shipState.AreaResRef;

                existingState.X =
                    shipState.X;

                existingState.Y =
                    shipState.Y;

                existingState.Z =
                    shipState.Z;

                existingState.Heading =
                    shipState.Heading;

                existingState.Underway =
                    shipState.Underway;

                existingState.Hull =
                    shipState.Hull;

                existingState.WeaponResRef =
                    string.IsNullOrWhiteSpace(
                        shipState.WeaponResRef)
                        ? "ship_cannon"
                        : shipState.WeaponResRef;

                Log.Info(
                    "Updating saved ship state for '{ShipName}'.",
                    shipState.ShipName);
            }

            await context.SaveChangesAsync();

            Log.Info(
                "Saved ship state: " +
                "Ship={ShipName}, Area={Area}, " +
                "X={X}, Y={Y}, Z={Z}, " +
                "Heading={Heading}, Underway={Underway}, " +
                "Hull={Hull}, Weapon={Weapon}",
                shipState.ShipName,
                shipState.AreaResRef,
                shipState.X,
                shipState.Y,
                shipState.Z,
                shipState.Heading,
                shipState.Underway,
                shipState.Hull,
                shipState.WeaponResRef);
        }
        catch (Exception e)
        {
            Log.Error(
                e,
                "Error saving ship state for '{ShipName}'.",
                shipState.ShipName);
        }
    }
}