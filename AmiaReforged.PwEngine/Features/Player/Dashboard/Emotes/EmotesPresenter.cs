using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Emotes;

public sealed class EmotesPresenter : ScryPresenter<EmotesView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    private EmotesModel Model { get; }

    [Inject]
    private Lazy<WindowDirector> WindowDirector { get; init; } = null!;

    public EmotesPresenter(EmotesView view, NwPlayer player)
    {
        _player = player;
        View = view;
        Model = new EmotesModel(player);
    }

    public override EmotesView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), null!)
        {
            Geometry = new NuiRect(115f, 145f, 350f, 320f),
            Transparent = true,
            Closable = false,
            Border = false,
            Collapsed = false,
            Resizable = false
        };
    }

    public override void Create()
    {
        if (_window == null)
        {
            _player.SendServerMessage(
                "The emotes window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        UpdateView();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        switch (obj.EventType)
        {
            case NuiEventType.Click:
                HandleClick(obj);
                break;
            case NuiEventType.Watch:
                HandleWatch(obj);
                break;
        }
    }

    private void HandleClick(ModuleEvents.OnNuiEvent click)
    {
        switch (click.ElementId)
        {
            case "btn_pick_target":
                HandlePickTarget();
                break;
            case "btn_perform_individual":
                HandlePerformIndividual();
                break;
            case "btn_perform_mutual":
                HandlePerformMutual();
                break;
        }
    }

    private void HandleWatch(ModuleEvents.OnNuiEvent watchEvent)
    {
        switch (watchEvent.ElementId)
        {
            case "combo_individual":
                Model.SelectedIndividualEmote = Token().GetBindValue(View.SelectedIndividual);
                break;
            case "combo_mutual":
                Model.SelectedMutualEmote = Token().GetBindValue(View.SelectedMutual);
                break;
        }
    }

    private void HandlePickTarget()
    {
        _player.EnterTargetMode(OnTargetSelected, new TargetModeSettings
        {
            CursorType = MouseCursor.Action,
            ValidTargets = ObjectTypes.Creature
        });
    }

    private void OnTargetSelected(ModuleEvents.OnPlayerTarget targetEvent)
    {
        if (targetEvent.TargetObject == null || !targetEvent.TargetObject.IsValid)
        {
            _player.SendServerMessage("Invalid target selected.", ColorConstants.Orange);
            return;
        }

        Model.SelectedTarget = targetEvent.TargetObject as NwGameObject;
        UpdateView();
    }

    private void HandlePerformIndividual()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.", ColorConstants.Red);
            return;
        }

        int emoteIndex;
        try
        {
            emoteIndex = Token().GetBindValue(View.SelectedIndividual);
        }
        catch
        {
            // If bind value is null or invalid, default to 0
            emoteIndex = 0;
        }

        if (emoteIndex < 0 || emoteIndex >= Model.IndividualEmotes.Count)
        {
            _player.SendServerMessage("Please select an emote.", ColorConstants.Orange);
            return;
        }

        EmoteOption selectedEmote = Model.IndividualEmotes[emoteIndex];

        // Determine the target - use selected target if it's the player's associate
        NwGameObject target = creature;
        if (Model.SelectedTarget != null && Model.SelectedTarget is NwCreature targetCreature)
        {
            // Check if target is an associate of the player
            if (targetCreature.Master == creature)
            {
                target = targetCreature;
            }
        }

        PerformIndividualEmote(target as NwCreature ?? creature, selectedEmote.Id);
    }

    private void HandlePerformMutual()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.", ColorConstants.Red);
            return;
        }

        // Check if a valid PC target is selected
        if (Model.SelectedTarget == null || Model.SelectedTarget is not NwCreature targetCreature)
        {
            _player.SendServerMessage("You must select a valid player character target for mutual emotes.", ColorConstants.Orange);
            return;
        }

        if (!targetCreature.IsPlayerControlled(out NwPlayer? targetPlayer) || targetPlayer == _player)
        {
            _player.SendServerMessage("You must select another player character for mutual emotes.", ColorConstants.Orange);
            return;
        }

        int emoteIndex;
        try
        {
            emoteIndex = Token().GetBindValue(View.SelectedMutual);
        }
        catch
        {
            // If bind value is null or invalid, default to 0
            emoteIndex = 0;
        }

        if (emoteIndex < 0 || emoteIndex >= Model.MutualEmotes.Count)
        {
            _player.SendServerMessage("Please select a mutual emote.", ColorConstants.Orange);
            return;
        }

        EmoteOption selectedEmote = Model.MutualEmotes[emoteIndex];

        // Request permission from the target player
        RequestMutualEmotePermission(targetCreature, selectedEmote);
    }

    private void RequestMutualEmotePermission(NwCreature target, EmoteOption emote)
    {
        // For now, just send a message - we'll need to implement a consent system later
        _player.SendServerMessage($"Requesting {emote.Name} with {target.Name}...", ColorConstants.Yellow);
        target.ControllingPlayer?.SendServerMessage(
            $"{_player.LoginCreature?.Name} wants to perform '{emote.Name}' with you. (Consent system coming soon)",
            ColorConstants.Yellow);

        // TODO: Implement proper consent dialog and execution
        _player.SendServerMessage("Mutual emote consent system is not yet implemented. Coming soon!", ColorConstants.Orange);
    }

    private void PerformIndividualEmote(NwCreature target, int emoteId)
    {
        TimeSpan loopDuration = TimeSpan.FromHours(2.77); // ~9999 seconds

        switch (emoteId)
        {
            case 1:
                target.PlayAnimation(Animation.FireForgetDodgeSide, 1.0f);
                break;
            case 2:
                target.PlayAnimation(Animation.FireForgetDrink, 1.0f);
                break;
            case 3:
                target.PlayAnimation(Animation.FireForgetDodgeDuck, 1.0f);
                break;
            case 4:
                target.PlayAnimation(Animation.LoopingDeadBack, 1.0f, true, loopDuration);
                break;
            case 5:
                target.PlayAnimation(Animation.LoopingDeadFront, 1.0f, true, loopDuration);
                break;
            case 6:
                target.PlayAnimation(Animation.FireForgetRead, 1.0f);
                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    await NwTask.SwitchToMainThread();
                    await target.PlayAnimation(Animation.FireForgetRead, 1.0f);
                });
                break;
            case 7:
                target.PlayAnimation(Animation.LoopingSitCross, 1.0f, true, loopDuration);
                break;
            case 11:
                target.PlayAnimation(Animation.LoopingTalkPleading, 1.0f, true, loopDuration);
                break;
            case 12:
                target.PlayAnimation(Animation.LoopingConjure1, 1.0f, true, loopDuration);
                break;
            case 13:
                target.PlayAnimation(Animation.LoopingConjure2, 1.0f, true, loopDuration);
                break;
            case 14:
                target.PlayAnimation(Animation.LoopingGetLow, 1.0f, true, loopDuration);
                break;
            case 15:
                target.PlayAnimation(Animation.LoopingGetMid, 1.0f, true, loopDuration);
                break;
            case 16:
                target.PlayAnimation(Animation.LoopingMeditate, 1.0f, true, loopDuration);
                break;
            case 17:
                target.PlayAnimation(Animation.LoopingTalkForceful, 1.0f, true, loopDuration);
                break;
            case 18:
                target.PlayAnimation(Animation.LoopingWorship, 1.0f, true, loopDuration);
                break;
            case 21:
                // Dance - complex sequence
                PerformDance(target);
                break;
            case 22:
                target.PlayAnimation(Animation.LoopingPauseDrunk, 1.0f, true, loopDuration);
                break;
            case 24:
                // Sit in nearest chair - TODO: implement chair finding logic
                _player.SendServerMessage("Sit in Chair not yet fully implemented.", ColorConstants.Orange);
                break;
            case 25:
                // Sit and drink
                target.PlayAnimation(Animation.LoopingSitCross, 1.0f, true, loopDuration);
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    await NwTask.SwitchToMainThread();
                    await target.PlayAnimation(Animation.FireForgetDrink, 1.0f);
                    await Task.Delay(2000);
                    await target.PlayAnimation(Animation.LoopingSitCross, 1.0f, true, loopDuration);
                });
                break;
            case 26:
                // Sit and read
                target.PlayAnimation(Animation.LoopingSitCross, 1.0f, true, loopDuration);
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    await NwTask.SwitchToMainThread();
                    await target.PlayAnimation(Animation.FireForgetRead, 1.0f);
                    await Task.Delay(2000);
                    await target.PlayAnimation(Animation.LoopingSitCross, 1.0f, true, loopDuration);
                });
                break;
            case 27:
                target.PlayAnimation(Animation.LoopingSpasm, 1.0f, true, loopDuration);
                break;
            case 28:
                // Smoke pipe - TODO: implement smoke pipe effect
                _player.SendServerMessage("Smoke Pipe not yet fully implemented.", ColorConstants.Orange);
                break;
            case 48:
                target.PlayAnimation(Animation.LoopingCustom19, 1.0f, true, loopDuration);
                break;
            default:
                _player.SendServerMessage("Unknown emote.", ColorConstants.Red);
                break;
        }
    }

    private void PerformDance(NwCreature target)
    {
        // Unequip weapons
        NwItem? rightHand = target.GetItemInSlot(InventorySlot.RightHand);
        NwItem? leftHand = target.GetItemInSlot(InventorySlot.LeftHand);

        if (rightHand != null) target.RunUnequip(rightHand);
        if (leftHand != null) target.RunUnequip(leftHand);

        // Dance sequence
        target.PlayAnimation(Animation.FireForgetVictory2, 1.0f);

        _ = Task.Run(async () =>
        {
            await Task.Delay(1000);
            await NwTask.SwitchToMainThread();
            target.PlayVoiceChat(VoiceChatType.Laugh);

            await Task.Delay(500);
            await target.PlayAnimation(Animation.LoopingTalkLaughing, 2.0f, true, TimeSpan.FromSeconds(2));

            await Task.Delay(2000);
            await target.PlayAnimation(Animation.FireForgetVictory1, 1.0f);

            await Task.Delay(1000);
            await target.PlayAnimation(Animation.FireForgetVictory3, 2.0f);

            await Task.Delay(2000);
            await target.PlayAnimation(Animation.LoopingGetMid, 3.0f, true, TimeSpan.FromSeconds(1));

            await Task.Delay(1000);
            await target.PlayAnimation(Animation.LoopingTalkForceful, 1.0f, true, TimeSpan.FromSeconds(1));

            await Task.Delay(1000);
            await target.PlayAnimation(Animation.FireForgetVictory2, 1.0f);

            // Re-equip weapons after dance
            await Task.Delay(7000);
            if (leftHand != null) target.RunEquip(leftHand, InventorySlot.LeftHand);
            if (rightHand != null) target.RunEquip(rightHand, InventorySlot.RightHand);
        });
    }

    public override void UpdateView()
    {
        // Update target name - show first two words (includes titles)
        string fullName = Model.SelectedTarget?.Name ?? _player.LoginCreature?.Name ?? "Self";
        string[] nameParts = fullName.Split(' ');
        string targetName = nameParts.Length > 1
            ? $"{nameParts[0]} {nameParts[1]}"
            : nameParts[0]; // If only one word, just show it
        Token().SetBindValue(View.TargetName, targetName);

        // Populate individual emotes combo - use index as value so selection works correctly
        List<NuiComboEntry> individualEntries = Model.IndividualEmotes
            .Select((e, index) => new NuiComboEntry(e.Name, index))
            .ToList();
        Token().SetBindValue(View.IndividualEmoteOptions, individualEntries);

        // Populate mutual emotes combo - use index as value so selection works correctly
        List<NuiComboEntry> mutualEntries = Model.MutualEmotes
            .Select((e, index) => new NuiComboEntry(e.Name, index))
            .ToList();
        Token().SetBindValue(View.MutualEmoteOptions, mutualEntries);

        // Initialize combo selections to 0 (first item) to prevent null errors
        Token().SetBindValue(View.SelectedIndividual, 0);
        Token().SetBindValue(View.SelectedMutual, 0);

        // Enable/disable mutual emote button based on target
        bool canPerformMutual = Model.SelectedTarget != null
            && Model.SelectedTarget is NwCreature targetCreature
            && targetCreature.IsPlayerControlled(out NwPlayer? targetPlayer)
            && targetPlayer != _player;

        Token().SetBindValue(View.PerformButtonEnabled, canPerformMutual);

        // Set up watches
        Token().SetBindWatch(View.SelectedIndividual, true);
        Token().SetBindWatch(View.SelectedMutual, true);
    }

    public override void Close()
    {
        _token.Close();
    }
}
