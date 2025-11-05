using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls.Nui;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;

/// <summary>
/// Wires player stall placeables to the stall claim workflow and performs configuration sanity checks.
/// </summary>
[ServiceBinding(typeof(PlayerStallInitializer))]
public sealed class PlayerStallInitializer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string PlayerStallTagPrefix = "engine_player_stall";
    private const string DefinedDbTagLocalString = "engine_player_stall_dbtag";

    private readonly IPlayerShopRepository _shops;
    private readonly RuntimeCharacterService _characters;
    private readonly PlayerStallClaimFlow _claimFlow;
    private readonly ShopLocationResolver _locationResolver;
    private readonly RegionIndex _regions;
    private readonly WindowDirector _windowDirector;
    private readonly PlayerStallEventManager _eventManager;

    private readonly Dictionary<uint, StallRegistration> _registrations = new();
    private readonly HashSet<uint> _wiredPlaceables = new();

    public PlayerStallInitializer(
        IPlayerShopRepository shops,
        RuntimeCharacterService characters,
        PlayerStallClaimFlow claimFlow,
        ShopLocationResolver locationResolver,
        RegionIndex regions,
        WindowDirector windowDirector,
        PlayerStallEventManager eventManager)
    {
        _shops = shops ?? throw new ArgumentNullException(nameof(shops));
        _characters = characters ?? throw new ArgumentNullException(nameof(characters));
        _claimFlow = claimFlow ?? throw new ArgumentNullException(nameof(claimFlow));
        _locationResolver = locationResolver ?? throw new ArgumentNullException(nameof(locationResolver));
        _regions = regions ?? throw new ArgumentNullException(nameof(regions));
        _windowDirector = windowDirector ?? throw new ArgumentNullException(nameof(windowDirector));
        _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));

        NwModule.Instance.OnModuleLoad += HandleModuleLoad;
    }

    private void RegisterExistingStalls()
    {
        foreach (NwPlaceable placeable in NwObject.FindObjectsWithTag<NwPlaceable>(PlayerStallTagPrefix))
        {
            TryRegisterPlaceable(placeable, revalidate: false);
        }
    }

    private void HandleModuleLoad(ModuleEvents.OnModuleLoad _)
    {
        try
        {
            RegisterExistingStalls();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to register player stall placeables during module load.");
        }
    }

    private void TryRegisterPlaceable(NwPlaceable placeable, bool revalidate)
    {
        if (placeable is null)
        {
            return;
        }

        if (!placeable.IsValid)
        {
            return;
        }

        if (revalidate)
        {
            _wiredPlaceables.Add(placeable.ObjectId);
        }
        else if (!_wiredPlaceables.Add(placeable.ObjectId))
        {
            return;
        }

        LocalVariableString stallTagVar = placeable.GetObjectVariable<LocalVariableString>(DefinedDbTagLocalString);
        string? dbTag = stallTagVar.Value;

        StallRegistration registration = BuildRegistration(placeable, dbTag);
        _registrations[placeable.ObjectId] = registration;

        placeable.OnUsed -= HandleStallUse;
        placeable.OnUsed += HandleStallUse;

        if (registration.State != StallRegistrationState.Ready)
        {
            Log.Warn("Player stall placeable {Tag} in area {AreaResRef} registered with state {State}.",
                placeable.Tag ?? "<no-tag>", registration.AreaResRef ?? "<unknown>", registration.State);
        }
    }

    private StallRegistration BuildRegistration(NwPlaceable placeable, string? dbTag)
    {
        if (string.IsNullOrWhiteSpace(dbTag))
        {
            return StallRegistration.Misconfigured(null, null, StallRegistrationState.MissingLocalTag);
        }

        NwArea? area = placeable.Area;
        if (area is null)
        {
            return StallRegistration.Misconfigured(dbTag, null, StallRegistrationState.MissingAreaContext);
        }

        string areaResRef = area.ResRef;

        List<PlayerStall> matches = _shops.ShopsByTag(dbTag);

        if (matches.Count == 0)
        {
            PlayerStall? seeded = TrySeedStallRecord(placeable, dbTag, areaResRef);
            if (seeded is null)
            {
                return StallRegistration.Misconfigured(dbTag, areaResRef, StallRegistrationState.SeedingFailed);
            }

            matches = new List<PlayerStall> { seeded };
        }

        if (matches.Count > 1)
        {
            return StallRegistration.Misconfigured(dbTag, areaResRef, StallRegistrationState.DuplicateTag);
        }

        PlayerStall stall = matches[0];

        if (!string.Equals(stall.AreaResRef, areaResRef, StringComparison.OrdinalIgnoreCase))
        {
            return StallRegistration.Misconfigured(dbTag, areaResRef, StallRegistrationState.AreaMismatch);
        }

        return StallRegistration.Ready(stall.Id, dbTag, areaResRef);
    }

    private PlayerStall? TrySeedStallRecord(NwPlaceable placeable, string dbTag, string areaResRef)
    {
        try
        {
            PlayerStall seeded = new()
            {
                Tag = dbTag,
                AreaResRef = areaResRef,
                SettlementTag = TryResolveSettlementTag(placeable, dbTag, areaResRef),
                LeaseStartUtc = DateTime.UtcNow,
                NextRentDueUtc = DateTime.UtcNow,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow,
                IsActive = true,
                SuspendedUtc = null,
                DeactivatedUtc = null
            };

            _shops.CreateShop(seeded);

            Log.Info("Seeded player stall {Tag} (ID {StallId}) for area {AreaResRef}.", dbTag, seeded.Id, areaResRef);
            return seeded;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to seed player stall {Tag} for area {AreaResRef}.", dbTag, areaResRef);
            return null;
        }
    }

    private string? TryResolveSettlementTag(NwPlaceable placeable, string dbTag, string areaResRef)
    {
        try
        {
            string? areaTag = placeable.Area?.Tag;
            string? areaName = placeable.Area?.Name;
            string displayName = string.IsNullOrWhiteSpace(placeable.Name) ? dbTag : placeable.Name;

            if (_locationResolver.TryResolve(
                    dbTag,
                    displayName,
                    placeable.Tag,
                    null,
                    areaResRef,
                    areaTag,
                    areaName,
                    out ShopLocationMetadata metadata))
            {
                if (metadata.SettlementId.Value > 0)
                {
                    return metadata.SettlementId.Value.ToString(CultureInfo.InvariantCulture);
                }

                if (!string.IsNullOrWhiteSpace(metadata.Settlement.Value))
                {
                    return metadata.Settlement.Value;
                }
            }

            if (!string.IsNullOrWhiteSpace(areaResRef))
            {
                try
                {
                    AreaTag areaDefinitionTag = new(areaResRef);
                    if (_regions.TryGetSettlementForArea(areaDefinitionTag, out SettlementId settlement) && settlement.Value > 0)
                    {
                        return settlement.Value.ToString(CultureInfo.InvariantCulture);
                    }
                }
                catch (Exception areaEx)
                {
                    Log.Debug(areaEx, "Failed parsing area resref {AreaResRef} while resolving settlement for stall {Tag}.", areaResRef, dbTag);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to resolve settlement tag while seeding stall {Tag}.", dbTag);
            return null;
        }
    }

    private void HandleStallUse(PlaceableEvents.OnUsed eventData)
    {
        _ = HandleStallUseAsync(eventData);
    }

    private async Task HandleStallUseAsync(PlaceableEvents.OnUsed eventData)
    {
        try
        {
            if (eventData.Placeable is not { IsValid: true } placeable)
            {
                return;
            }

            if (!eventData.UsedBy.IsPlayerControlled(out NwPlayer? player))
            {
                return;
            }

            if (player is null || !player.IsValid)
            {
                return;
            }

            if (player.LoginCreature is null)
            {
                await SendServerMessageAsync(player,
                    "We couldn't confirm your identity for this stall. Please relog and try again.",
                    ColorConstants.Orange).ConfigureAwait(false);
                return;
            }

            StallRegistration registration = await ResolveRegistrationAsync(placeable).ConfigureAwait(false);

            if (registration.State != StallRegistrationState.Ready)
            {
                await NotifyMisconfigurationAsync(player, registration).ConfigureAwait(false);
                return;
            }

            PlayerStall? stall = await Task.Run(() =>
                _shops.GetShopById(registration.StallId!.Value)).ConfigureAwait(false);

            if (stall is null)
            {
                Log.Warn("Player stall record {Tag} disappeared before interaction.", registration.DbTag);
                StallRegistration updated = StallRegistration.Misconfigured(registration.DbTag, registration.AreaResRef, StallRegistrationState.DatabaseRecordMissing);
                _registrations[placeable.ObjectId] = updated;
                await NotifyMisconfigurationAsync(player, updated).ConfigureAwait(false);
                return;
            }

            if (!TryResolvePersona(player, out PersonaId personaId, out Guid ownerGuid))
            {
                await SendServerMessageAsync(player,
                    "We couldn't verify your persona for stall leasing. Please relog and try again.",
                    ColorConstants.Orange).ConfigureAwait(false);
                return;
            }

            bool ownsStall = IsOwnedByCurrentPersona(stall, ownerGuid, personaId);
            bool stallHasOwner = StallHasOwner(stall);

            if (ownsStall)
            {
                if (IsPersonaUsingDifferentCharacter(stall, player, personaId))
                {
                    string ownerName = string.IsNullOrWhiteSpace(stall.OwnerDisplayName)
                        ? "another character"
                        : stall.OwnerDisplayName!;

                    await SendServerMessageAsync(player,
                            $"Your persona already operates this stall as {ownerName}. Swap to that character to manage it.",
                            ColorConstants.Orange)
                        .ConfigureAwait(false);
                    await SendFloatingTextAsync(player,
                            "You cannot interact with this stall on this character.")
                        .ConfigureAwait(false);
                    return;
                }

                await OpenSellerWindowAsync(player, stall, personaId).ConfigureAwait(false);
                return;
            }

            if (stallHasOwner)
            {
                if (!IsStallOpen(stall))
                {
                    await SendServerMessageAsync(player,
                            "This stall is currently closed to customers.",
                            ColorConstants.Orange)
                        .ConfigureAwait(false);
                    await SendFloatingTextAsync(player,
                            "The stall appears to be closed.")
                        .ConfigureAwait(false);
                    return;
                }

                await OpenBuyerWindowAsync(player, stall, personaId).ConfigureAwait(false);
                return;
            }

            bool alreadyOwnsInArea = await Task.Run(() =>
                _shops.HasActiveOwnershipInArea(ownerGuid, registration.AreaResRef!, stall.Id)).ConfigureAwait(false);

            if (alreadyOwnsInArea)
            {
                await SendServerMessageAsync(player,
                        "You already manage a stall in this marketplace.",
                        ColorConstants.Orange)
                    .ConfigureAwait(false);
                return;
            }

            await BeginClaimFlowAsync(player, placeable, stall, personaId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            try
            {
                Log.Error(ex, "Unhandled error while handling player stall interaction.");
            }
            catch
            {
                // Swallow logging exceptions to avoid crashing the event pipeline.
            }
        }
    }

    private async Task<StallRegistration> ResolveRegistrationAsync(NwPlaceable placeable)
    {
        if (_registrations.TryGetValue(placeable.ObjectId, out StallRegistration existing))
        {
            return existing;
        }

        TryRegisterPlaceable(placeable, revalidate: true);
        return _registrations.TryGetValue(placeable.ObjectId, out StallRegistration updated)
            ? updated
            : StallRegistration.Misconfigured(null, placeable.Area?.ResRef, StallRegistrationState.MissingLocalTag);
    }

    private static bool IsOwnedByCurrentPersona(PlayerStall stall, Guid ownerGuid, PersonaId personaId)
    {
        if (stall.OwnerCharacterId.HasValue && stall.OwnerCharacterId.Value == ownerGuid)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(stall.OwnerPersonaId))
        {
            return string.Equals(stall.OwnerPersonaId, personaId.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool StallHasOwner(PlayerStall stall)
    {
        bool hasCharacterOwner = stall.OwnerCharacterId.HasValue && stall.OwnerCharacterId.Value != Guid.Empty;
        bool hasPersonaOwner = !string.IsNullOrWhiteSpace(stall.OwnerPersonaId);
        return hasCharacterOwner || hasPersonaOwner;
    }

    private static bool IsStallOpen(PlayerStall stall)
    {
        return stall.IsActive && !stall.SuspendedUtc.HasValue;
    }

    private static bool IsPersonaUsingDifferentCharacter(PlayerStall stall, NwPlayer player, PersonaId personaId)
    {
        if (player.LoginCreature is null || !player.LoginCreature.IsValid)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(stall.OwnerPersonaId))
        {
            return false;
        }

        string personaKey = personaId.ToString();
        if (!string.Equals(stall.OwnerPersonaId, personaKey, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string? ownerName = stall.OwnerDisplayName;
        string? currentName = player.LoginCreature.Name;

        if (string.IsNullOrWhiteSpace(ownerName) || string.IsNullOrWhiteSpace(currentName))
        {
            return false;
        }

        return !string.Equals(ownerName.Trim(), currentName.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private bool TryResolvePersona(NwPlayer player, out PersonaId personaId, out Guid ownerGuid)
    {
        personaId = default;
        ownerGuid = Guid.Empty;

        if (!_characters.TryGetPlayerKey(player, out Guid key) || key == Guid.Empty)
        {
            Log.Warn("Failed to resolve persistent key for player {PlayerName} while handling stall interaction.",
                player.PlayerName);
            return false;
        }

        try
        {
            CharacterId characterId = CharacterId.From(key);
            ownerGuid = characterId.Value;
            personaId = PersonaId.FromCharacter(characterId);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to convert key {PlayerKey} into PersonaId for player {PlayerName}.",
                key,
                player.PlayerName);
            return false;
        }
    }

    private static async Task SendServerMessageAsync(NwPlayer player, string message, Color color)
    {
        await NwTask.SwitchToMainThread();

        if (player.IsValid)
        {
            player.SendServerMessage(message, color);
        }
    }

    private static async Task SendFloatingTextAsync(NwPlayer player, string message)
    {
        await NwTask.SwitchToMainThread();

        if (player.IsValid)
        {
            player.FloatingTextString(message, false);
        }
    }

    private async Task NotifyMisconfigurationAsync(NwPlayer player, StallRegistration registration)
    {
        string message = registration.State switch
        {
            StallRegistrationState.MissingLocalTag =>
                "This stall is missing its backend identifier. Please notify a DM.",
            StallRegistrationState.MissingAreaContext =>
                "This stall isn't linked to an area yet. Please notify a DM.",
            StallRegistrationState.DatabaseRecordMissing =>
                "This stall has not been provisioned in the economy backend. Please notify a DM.",
            StallRegistrationState.DuplicateTag =>
                "This stall shares a database tag with another stall. Please notify a DM.",
            StallRegistrationState.AreaMismatch =>
                "This stall is registered to a different area. Please notify a DM.",
            StallRegistrationState.SeedingFailed =>
                "We couldn't provision this stall automatically. Please notify a DM.",
            _ => "This stall is misconfigured. Please notify a DM."
        };

        await SendServerMessageAsync(player, message, ColorConstants.Orange).ConfigureAwait(false);
        await SendFloatingTextAsync(player, message).ConfigureAwait(false);
    }

    private async Task OpenBuyerWindowAsync(NwPlayer player, PlayerStall stall, PersonaId personaId)
    {
        PlayerStallBuyerSnapshot? snapshot = await _eventManager
            .BuildBuyerSnapshotForAsync(stall.Id, personaId, player)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            Log.Warn("Failed to build buyer snapshot for stall {StallId} while opening storefront.", stall.Id);
            await SendServerMessageAsync(player,
                    "We couldn't open the stall storefront. Please try again later.",
                    ColorConstants.Red)
                .ConfigureAwait(false);
            return;
        }

        string title = string.IsNullOrWhiteSpace(snapshot.Summary.StallName)
            ? "Market Stall"
            : snapshot.Summary.StallName;

        PlayerStallBuyerWindowConfig config = new(
            stall.Id,
            personaId,
            title,
            snapshot);

        PlayerBuyerView view = new(player, config);

        await NwTask.SwitchToMainThread();
        _windowDirector.CloseWindow(player, typeof(PlayerBuyerPresenter));
        _windowDirector.OpenWindow(view.Presenter);

        await SendServerMessageAsync(player,
                "Opening stall storefront.",
                ColorConstants.Cyan)
            .ConfigureAwait(false);
    }

    private async Task OpenSellerWindowAsync(NwPlayer player, PlayerStall stall, PersonaId personaId)
    {
        PlayerStallSellerSnapshot? snapshot = await _eventManager
            .BuildSellerSnapshotForAsync(stall.Id, personaId)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            Log.Warn("Failed to build seller snapshot for stall {StallId} while opening management window.", stall.Id);
            await SendServerMessageAsync(player,
                    "We couldn't open the stall management window. Please try again later.",
                    ColorConstants.Red)
                .ConfigureAwait(false);
            return;
        }

        string title = string.IsNullOrWhiteSpace(snapshot.Summary.StallName)
            ? "Stall Management"
            : snapshot.Summary.StallName;

        PlayerStallSellerWindowConfig config = new(
            stall.Id,
            personaId,
            title,
            snapshot);

        PlayerSellerView view = new(player, config);

        await NwTask.SwitchToMainThread();
        _windowDirector.CloseWindow(player, typeof(PlayerSellerPresenter));
        _windowDirector.OpenWindow(view.Presenter);

        await SendServerMessageAsync(player,
                "Opening stall management window.",
                ColorConstants.Cyan)
            .ConfigureAwait(false);
    }

    private async Task BeginClaimFlowAsync(NwPlayer player, NwPlaceable placeable, PlayerStall stall, PersonaId personaId)
    {
        await _claimFlow.BeginClaimAsync(player, placeable, stall, personaId).ConfigureAwait(false);
    }

    private enum StallRegistrationState
    {
        Ready,
        MissingLocalTag,
        MissingAreaContext,
        DatabaseRecordMissing,
        DuplicateTag,
        AreaMismatch,
        SeedingFailed
    }

    private sealed record StallRegistration(long? StallId, string? DbTag, string? AreaResRef, StallRegistrationState State)
    {
        public static StallRegistration Ready(long stallId, string dbTag, string areaResRef)
        {
            return new StallRegistration(stallId, dbTag, areaResRef, StallRegistrationState.Ready);
        }

        public static StallRegistration Misconfigured(string? dbTag, string? areaResRef, StallRegistrationState state)
        {
            return new StallRegistration(null, dbTag, areaResRef, state);
        }
    }
}
