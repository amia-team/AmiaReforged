using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls.Nui;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;

[ServiceBinding(typeof(MarketReeveInteractionService))]
internal sealed class MarketReeveInteractionService
{
    private const string ReeveDbTagLocalString = "engine_market_reeve_dbtag";
    private static readonly string[] CandidateTags = ["market_reeve", "engine_market_reeve"];
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly RuntimeCharacterService _characters;
    private readonly ReeveLockupService _lockup;
    private readonly WindowDirector _windowDirector;
    private readonly Dictionary<uint, ReeveRegistration> _registrations = new();

    public MarketReeveInteractionService(
        RuntimeCharacterService characters,
        ReeveLockupService lockup,
        WindowDirector windowDirector)
    {
        _characters = characters ?? throw new ArgumentNullException(nameof(characters));
        _lockup = lockup ?? throw new ArgumentNullException(nameof(lockup));
        _windowDirector = windowDirector ?? throw new ArgumentNullException(nameof(windowDirector));

        try
        {
            RegisterExistingReeves();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to register existing market reeves during initialization.");
        }

        NwModule.Instance.OnModuleLoad += HandleModuleLoad;
    }

    private void HandleModuleLoad(ModuleEvents.OnModuleLoad _)
    {
        try
        {
            RegisterExistingReeves();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to register market reeves during module load.");
        }
    }

    private void RegisterExistingReeves()
    {
        foreach (string tag in CandidateTags)
        {
            foreach (NwCreature candidate in NwObject.FindObjectsWithTag<NwCreature>(tag))
            {
                TryRegisterReeve(candidate);
            }
        }
    }

    private void TryRegisterReeve(NwCreature? npc)
    {
        if (npc is null || !npc.IsValid)
        {
            return;
        }

        if (npc.IsPlayerControlled)
        {
            return;
        }

        if (_registrations.ContainsKey(npc.ObjectId))
        {
            UpdateRegistration(npc);
            return;
        }

        string? dbTag = npc.GetObjectVariable<LocalVariableString>(ReeveDbTagLocalString).Value;
        string? areaResRef = npc.Area?.ResRef;

        _registrations[npc.ObjectId] = new ReeveRegistration(dbTag, areaResRef);

        npc.OnConversation -= HandleReeveConversation;
        npc.OnConversation += HandleReeveConversation;

        Log.Info("Registered market reeve {NpcName} ({Tag}) for area {AreaResRef}.",
            npc.Name,
            npc.Tag ?? "<no-tag>",
            areaResRef ?? "<unknown>");
    }

    private void UpdateRegistration(NwCreature npc)
    {
        if (!_registrations.TryGetValue(npc.ObjectId, out ReeveRegistration registration))
        {
            return;
        }

        string? areaResRef = npc.Area?.ResRef;
        if (string.Equals(registration.AreaResRef, areaResRef, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _registrations[npc.ObjectId] = registration with { AreaResRef = areaResRef };
    }

    private void HandleReeveConversation(CreatureEvents.OnConversation eventData)
    {
        _ = HandleReeveConversationAsync(eventData);
    }

    private async Task HandleReeveConversationAsync(CreatureEvents.OnConversation eventData)
    {
        NwCreature? npc = eventData.Creature;
        if (npc is null)
        {
            return;
        }

        if (!_registrations.TryGetValue(npc.ObjectId, out ReeveRegistration registration))
        {
            TryRegisterReeve(npc);
            if (!_registrations.TryGetValue(npc.ObjectId, out registration))
            {
                Log.Warn("Conversation triggered on unregistered market reeve {Tag}.", npc.Tag ?? "<no-tag>");
                return;
            }
        }

        (NwPlayer? player, NwCreature? playerCreature) = ResolvePlayer(npc);
        if (player is null || playerCreature is null)
        {
            return;
        }

        if (!TryResolvePersona(player, out PersonaId personaId))
        {
            await SendServerMessageAsync(player,
                    "We couldn't verify your persona for market reeve services.",
                    ColorConstants.Red)
                .ConfigureAwait(false);
            return;
        }

        string windowTitle = BuildWindowTitle(npc);

        string? areaResRef = registration.AreaResRef ?? npc.Area?.ResRef;
        if (string.IsNullOrWhiteSpace(registration.AreaResRef) && !string.IsNullOrWhiteSpace(areaResRef))
        {
            _registrations[npc.ObjectId] = registration with { AreaResRef = areaResRef };
        }

        IReadOnlyList<ReeveLockupItemSummary> items;
        try
        {
            items = await _lockup.ListStoredInventoryAsync(personaId, areaResRef).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load reeve lockup inventory for persona {Persona}.", personaId);
            await SendServerMessageAsync(player,
                    "The reeve cannot locate your stored items right now.",
                    ColorConstants.Red)
                .ConfigureAwait(false);
            return;
        }

        await NwTask.SwitchToMainThread();

        if (!player.IsValid || playerCreature is null || !playerCreature.IsValid || npc is null || !npc.IsValid)
        {
            return;
        }

        MarketReeveLockupWindowConfig config = new(
            personaId,
            areaResRef,
            windowTitle,
            items,
            playerCreature);

        MarketReeveLockupView view = new(player, config);

        _windowDirector.CloseWindow(player, typeof(MarketReeveLockupPresenter));
        _windowDirector.OpenWindow(view.Presenter);
    }

    private static (NwPlayer? Player, NwCreature? Creature) ResolvePlayer(NwCreature npc)
    {
        foreach (NwCreature candidate in npc.GetNearestCreatures(CreatureTypeFilter.Perception(PerceptionType.Seen)))
        {
            if (candidate.IsLoginPlayerCharacter(out NwPlayer? player))
            {
                return (player, candidate);
            }
        }

        return (null, null);
    }

    private bool TryResolvePersona(NwPlayer player, out PersonaId personaId)
    {
        personaId = default;

        if (!_characters.TryGetPlayerKey(player, out Guid key) || key == Guid.Empty)
        {
            Log.Warn("Failed to resolve persistent key for player {PlayerName} while using market reeve services.",
                player.PlayerName);
            return false;
        }

        try
        {
            CharacterId characterId = CharacterId.From(key);
            personaId = PersonaId.FromCharacter(characterId);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn(ex,
                "Failed to convert persistent key {PlayerKey} into persona for player {PlayerName}.",
                key,
                player.PlayerName);
            return false;
        }
    }

    private static string BuildWindowTitle(NwCreature npc)
    {
        if (!string.IsNullOrWhiteSpace(npc.Name))
        {
            return npc.Name;
        }

        string? tag = npc.Tag;
        return string.IsNullOrWhiteSpace(tag) ? "Market Reeve Lockup" : $"Market Reeve ({tag})";
    }

    private static async Task SendServerMessageAsync(NwPlayer player, string message, Color color)
    {
        await NwTask.SwitchToMainThread();

        if (player.IsValid)
        {
            player.SendServerMessage(message, color);
        }
    }

    private readonly record struct ReeveRegistration(string? DbTag, string? AreaResRef);
}
