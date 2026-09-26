using System.Linq;
using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Nui;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Nui.Helpers;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;

/// <summary>
///     Runtime service that decides whether to show the trait-selection onboarding prompt when a
///     character enters the server starting area.
/// </summary>
/// <remarks>
///     This service subscribes to the starting area's <see cref="AreaEvents.OnEnter" /> (never to
///     <see cref="NwModule.Instance.OnClientEnter" />) and orchestrates the full gating sequence:
///     player/login checks, PC-key resolution, eligibility, preference, and duplicate-window
///     protection, before opening the <see cref="TraitOnboardingPromptView" />.
///     <para>
///     A failure at any stage is logged and swallowed so the area event never crashes, and so no
///     player teleport or character-registration interrupt is triggered.
///     </para>
/// </remarks>
[ServiceBinding(typeof(TraitOnboardingService))]
public class TraitOnboardingService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string DisabledReminderMessage =
        "Trait selection reminders disabled. You can select traits at any time from the Traits section of your Codex.";

    private readonly ICharacterTraitRepository _characterTraitRepository;
    private readonly WindowDirector _windowDirector;
    private readonly TraitSelectionWindowService _selectionWindowService;

    public TraitOnboardingService(
        ICharacterTraitRepository characterTraitRepository,
        WindowDirector windowDirector,
        TraitSelectionWindowService selectionWindowService)
    {
        _characterTraitRepository = characterTraitRepository;
        _windowDirector = windowDirector;
        _selectionWindowService = selectionWindowService;

        NwArea entryArea = NwModule.Instance.StartingLocation.Area;
        entryArea.OnEnter += OnEnter;

        Log.Info("TraitOnboardingService initialized on starting area OnEnter.");
    }

    /// <summary>
    ///     Handles a game object entering the starting area, gating the trait onboarding prompt.
    /// </summary>
    private void OnEnter(AreaEvents.OnEnter obj)
    {
        try
        {
            // Guard 1 & 2: require a player-controlled login character and resolve its NwPlayer.
            if (!obj.EnteringObject.IsLoginPlayerCharacter(out NwPlayer? player)) return;

            // Guard 3: ignore DMs.
            if (player.IsDM) return;

            // Guard 4: require a non-null login creature.
            NwCreature? creature = player.LoginCreature;
            if (creature is null) return;

            // Guard 5 & 6: require an existing ds_pckey in the login creature's inventory.
            NwItem? pcKey = creature.Inventory.Items.FirstOrDefault(i => i.ResRef == "ds_pckey");
            if (pcKey is null) return;

            // Guard 7 & 8: resolve the character GUID and bail out if it is empty.
            Guid characterId = PcKeyUtils.GetPcKey(player);
            if (characterId == Guid.Empty) return;

            // Eligibility: skip characters that have already confirmed a trait.
            List<CharacterTrait> characterTraits =
                _characterTraitRepository.GetByCharacterId(CharacterId.From(characterId));

            if (!TraitOnboardingEligibility.IsEligible(characterTraits)) return;

            // Preference: skip characters that have opted out of the reminder.
            if (TraitOnboardingPreference.IsDisabled(pcKey)) return;

            // Duplicate protection: never open the prompt twice for the same player.
            if (_windowDirector.IsWindowOpen(player, typeof(TraitOnboardingPromptPresenter))) return;

            // Open the prompt, delegating the Yes path to the shared selection-window service.
            TraitOnboardingPromptView view = new(
                player,
                onYes: () => _selectionWindowService.Open(player),
                onSuppressionRequested: () => OnSuppressionRequested(pcKey, player));

            _windowDirector.OpenWindow(view.Presenter);
        }
        catch (Exception ex)
        {
            // Never crash the area event, teleport the player, or interrupt character registration.
            Log.Error(ex, "TraitOnboardingService failed to process area enter; onboarding skipped.");
        }
    }

    /// <summary>
    ///     Handles the prompt's suppression ("Don't show again") callback.
    /// </summary>
    private void OnSuppressionRequested(NwItem pcKey, NwPlayer player)
    {
        TraitOnboardingPreference.Disable(pcKey);
        player.SendServerMessage(DisabledReminderMessage);
    }
}
