using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.Characters.Runtime;
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

    private readonly Dictionary<uint, StallRegistration> _registrations = new();
    private readonly HashSet<uint> _wiredPlaceables = new();

    public PlayerStallInitializer(IPlayerShopRepository shops, RuntimeCharacterService characters)
    {
        _shops = shops ?? throw new ArgumentNullException(nameof(shops));
        _characters = characters ?? throw new ArgumentNullException(nameof(characters));

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
            return StallRegistration.Misconfigured(dbTag, areaResRef, StallRegistrationState.DatabaseRecordMissing);
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

            if (IsOwnedByDifferentPersona(stall, ownerGuid, personaId))
            {
                string ownerName = string.IsNullOrWhiteSpace(stall.OwnerDisplayName)
                    ? "another proprietor"
                    : stall.OwnerDisplayName!;

                await SendServerMessageAsync(player,
                        $"This stall is already operated by {ownerName}.",
                        ColorConstants.Orange)
                    .ConfigureAwait(false);
                return;
            }

            if (IsOwnedByCurrentPersona(stall, ownerGuid, personaId))
            {
                await SendServerMessageAsync(player,
                        "You already manage this stall.",
                        ColorConstants.Cyan)
                    .ConfigureAwait(false);
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

    private static bool IsOwnedByDifferentPersona(PlayerStall stall, Guid ownerGuid, PersonaId personaId)
    {
        if (stall.OwnerCharacterId.HasValue)
        {
            return stall.OwnerCharacterId.Value != ownerGuid;
        }

        if (!string.IsNullOrWhiteSpace(stall.OwnerPersonaId))
        {
            return !string.Equals(stall.OwnerPersonaId, personaId.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        return false;
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
            _ => "This stall is misconfigured. Please notify a DM."
        };

        await SendServerMessageAsync(player, message, ColorConstants.Orange).ConfigureAwait(false);
        await SendFloatingTextAsync(player, message).ConfigureAwait(false);
    }

    private async Task BeginClaimFlowAsync(NwPlayer player, NwPlaceable placeable, PlayerStall stall, PersonaId personaId)
    {
        await SendServerMessageAsync(player,
                "The stall appears to be available. Stall claim UI is not yet implemented.",
                ColorConstants.Yellow)
            .ConfigureAwait(false);

        Log.Info("Player {PlayerName} initiated stall claim for stall {StallId} ({Tag}) in area {AreaResRef}.",
            player.PlayerName,
            stall.Id,
            stall.Tag,
            placeable.Area?.ResRef ?? "<unknown>");
    }

    private enum StallRegistrationState
    {
        Ready,
        MissingLocalTag,
        MissingAreaContext,
        DatabaseRecordMissing,
        DuplicateTag,
        AreaMismatch
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
