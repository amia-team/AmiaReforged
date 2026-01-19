using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
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
            Geometry = new NuiRect(25f, 90f, 350f, 320f),
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

        // Save the current Z translation to PC key for later restoration
        NwCreature? creature = _player.LoginCreature;
        if (creature != null)
        {
            string pcKey = creature.GetObjectVariable<LocalVariableString>("pc_key").Value ?? "";
            float currentZ = creature.VisualTransform.Translation.Z;
            creature.GetObjectVariable<LocalVariableFloat>($"{pcKey}_emote_saved_z").Value = currentZ;
        }

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
            case "btn_transform":
                HandleTransformButton();
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

        // Validate that the target is acceptable
        if (targetEvent.TargetObject is NwCreature targetCreature)
        {
            NwCreature? playerCreature = _player.LoginCreature;

            // Check if target is: self, an associate, a bottled companion, or another player
            bool isSelf = targetCreature == playerCreature;
            bool isAssociate = playerCreature != null && targetCreature.Master == playerCreature;
            bool isBottledCompanion = IsBottledCompanion(targetCreature);
            bool isOtherPlayer = targetCreature.IsPlayerControlled(out NwPlayer? _);

            if (isSelf || isAssociate || isBottledCompanion || isOtherPlayer)
            {
                Model.SelectedTarget = targetEvent.TargetObject as NwGameObject;
                UpdateView();
            }
            else
            {
                _player.SendServerMessage("You can only target yourself, your associates, your bottled companions, or another player.", ColorConstants.Orange);
            }
        }
        else
        {
            _player.SendServerMessage("You can only target yourself, your associates, your bottled companions, or another player.", ColorConstants.Orange);
        }
    }

    private bool IsBottledCompanion(NwCreature creature)
    {
        NwItem? pcKey = _player.ControlledCreature?.Inventory.Items.FirstOrDefault(item => item.Tag == "ds_pckey");

        if (pcKey == null) return false;

        string publicKey = pcKey.Name.Length >= 8 ? pcKey.Name.Substring(0, 8) : pcKey.Name;
        string expectedTag = $"ds_npc_{publicKey}";
        return creature.Tag == expectedTag;
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

        // Determine the target - use selected target if it's the player's associate or bottled companion
        NwGameObject target = creature;
        if (Model.SelectedTarget != null && Model.SelectedTarget is NwCreature targetCreature)
        {
            // Check if target is an associate of the player OR a bottled companion
            if (targetCreature.Master == creature || IsBottledCompanion(targetCreature))
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
        NwPlayer? targetPlayer = target.ControllingPlayer;
        if (targetPlayer == null || _player.LoginCreature == null)
        {
            _player.SendServerMessage("Failed to send emote request. Target player not found.", ColorConstants.Red);
            return;
        }

        // Create and show the consent popup to the target player
        EmoteConsentView consentView = new();
        EmoteConsentPresenter consentPresenter = new(
            consentView,
            targetPlayer,
            _player,
            _player.LoginCreature,
            emote,
            (consented) =>
            {
                if (consented)
                {
                    // Perform the mutual emote
                    PerformMutualEmote(_player.LoginCreature, target, emote.Id);
                }
            });

        // Get WindowDirector from AnvilCore service locator
        WindowDirector? windowDirector = AnvilCore.GetService<WindowDirector>();
        if (windowDirector == null)
        {
            _player.SendServerMessage("Failed to open consent window. Please report this bug.", ColorConstants.Red);
            return;
        }

        // Use WindowDirector to open the consent window
        windowDirector.OpenWindow(consentPresenter);

        // Notify the requester
        _player.SendServerMessage($"Requesting {emote.Name} with {target.Name}...", ColorConstants.Yellow);
    }

    private void PerformMutualEmote(NwCreature user, NwCreature target, int emoteId)
    {
        switch (emoteId)
        {
            case 38: // Kiss Standing
                PerformKiss(target, user, false);
                break;
            case 39: // Kiss Lying Down
                PerformKissLyingDown(user, target);
                break;
            case 40: // Hug
                PerformKiss(target, user, true);
                break;
            case 41: // Hug from Behind
                PerformGetCloseTo(user, target, true);
                break;
            case 42: // Back Close
                PerformGetCloseTo(user, target, false);
                break;
            case 43: // Waltz
                PerformWaltz(user, target);
                break;
            case 44: // Lap Sit (Ground, Facing)
                PerformLapSit(user, target, true);
                break;
            case 45: // Lap Sit (Ground, Away)
                PerformLapSit(user, target, false);
                break;
            case 46: // Lap Sit (Chair, Facing)
            case 47: // Lap Sit (Chair, Away)
                // Chair lap sit is more complex and requires placeable creation
                // For now, use ground lap sit behavior
                PerformLapSit(user, target, emoteId == 46);
                break;
            default:
                _player.SendServerMessage("Unknown mutual emote.", ColorConstants.Red);
                break;
        }
    }

    // Helper method to calculate opposite direction
    private float GetOppositeDirection(float facing)
    {
        return (facing + 180.0f) % 360.0f;
    }

    // Helper method to normalize direction
    private float NormalizeDirection(float angle)
    {
        while (angle < 0) angle += 360.0f;
        while (angle >= 360.0f) angle -= 360.0f;
        return angle;
    }

    private void PerformKiss(NwCreature kisser, NwCreature kissee, bool isHug)
    {
        TimeSpan ghostDuration = TimeSpan.FromSeconds(8);
        TimeSpan emoteDuration = TimeSpan.FromSeconds(9999);

        // Apply ghost effect (makes them non-blocking)
        Effect ghost = Effect.CutsceneGhost();
        kisser.ApplyEffect(EffectDuration.Temporary, ghost, ghostDuration);
        kissee.ApplyEffect(EffectDuration.Temporary, ghost, ghostDuration);

        Location? kisseeLocation = kissee.Location;
        if (kisseeLocation == null) return;

        Location? kisserSaveLocation = kisser.Location;

        // Calculate distance based on gender
        float kisserDistance = 1.0f;
        if (kisser.Gender == kissee.Gender && kisser.Gender == Gender.Male)
            kisserDistance += 0.34f;
        else if (kisser.Gender == kissee.Gender && kisser.Gender == Gender.Female)
            kisserDistance -= 0.3f;

        float kisseeFacing = kisseeLocation.Rotation;
        float kisserFacing = GetOppositeDirection(kisseeFacing);

        // Calculate kisser position
        System.Numerics.Vector3 kisseePos = kisseeLocation.Position;
        System.Numerics.Vector3 direction = new System.Numerics.Vector3(
            (float)Math.Cos(kisseeFacing * Math.PI / 180.0f),
            (float)Math.Sin(kisseeFacing * Math.PI / 180.0f),
            0f
        );
        System.Numerics.Vector3 kisserPos = kisseePos + direction * kisserDistance;
        Location kisserLocation = Location.Create(kisseeLocation.Area, kisserPos, kisserFacing);

        // Move kisser to position
        kisser.ActionJumpToLocation(kisserLocation);

        // Perform animations after delay
        Animation animation = isHug ? Animation.LoopingCustom14 : Animation.LoopingCustom11;

        Task.Run(async () =>
        {
            await Task.Delay(3000);
            await NwTask.SwitchToMainThread();

            await kisser.PlayAnimation(animation, 1.0f, true, emoteDuration);
            await kissee.PlayAnimation(animation, 1.0f, true, emoteDuration);

            // Return to saved location after duration
            if (kisserSaveLocation != null)
            {
                await Task.Delay((int)emoteDuration.TotalMilliseconds);
                await kisser.ActionJumpToLocation(kisserSaveLocation);
            }
        }).ContinueWith(t => { if (t.IsFaulted && t.Exception != null) throw t.Exception; });
    }

    private void PerformKissLyingDown(NwCreature user, NwCreature target)
    {
        NwCreature kisser, kissee;
        Animation kisserAnim, kisseeAnim;

        // Male lies on top
        if (target.Gender == Gender.Male)
        {
            kissee = user;
            kisser = target;
        }
        else
        {
            kisser = user;
            kissee = target;
        }

        // Set animations based on gender
        kisserAnim = kisser.Gender == Gender.Female ? Animation.LoopingCustom13 : Animation.LoopingCustom12;
        kisseeAnim = kissee.Gender == Gender.Male ? Animation.LoopingCustom13 : Animation.LoopingCustom12;

        // Calculate distance based on race
        float kisserDistance = 0.25f;
        if ((kisser.Race.RacialType == RacialType.Human && kissee.Race.RacialType == RacialType.HalfElf) ||
            (kisser.Race.RacialType == RacialType.HalfElf && kissee.Race.RacialType == RacialType.Human) ||
            (kisser.Race.RacialType == kissee.Race.RacialType))
        {
            kisserDistance = 0.25f;
        }
        else if (kissee.Race.RacialType == RacialType.Elf)
            kisserDistance = 0.4f;
        else if (kisser.Race.RacialType == RacialType.Elf)
            kisserDistance = 0.15f;

        // Adjust for same gender
        if (kisser.Gender == kissee.Gender)
        {
            if (kisser.Gender == Gender.Female) kisserDistance += 0.1f;
            else kisserDistance -= 0.1f;
        }

        TimeSpan ghostDuration = TimeSpan.FromSeconds(8);
        Effect ghost = Effect.CutsceneGhost();
        kisser.ApplyEffect(EffectDuration.Temporary, ghost, ghostDuration);
        kissee.ApplyEffect(EffectDuration.Temporary, ghost, ghostDuration);

        Location? targetLocation = target.Location;
        if (targetLocation == null) return;

        float kisserFacing = targetLocation.Rotation;
        float kisseeFacing = GetOppositeDirection(kisserFacing);

        // Calculate positions
        System.Numerics.Vector3 targetPos = targetLocation.Position;
        System.Numerics.Vector3 direction = new System.Numerics.Vector3(
            (float)Math.Cos(kisserFacing * Math.PI / 180.0f),
            (float)Math.Sin(kisserFacing * Math.PI / 180.0f),
            0f
        );
        System.Numerics.Vector3 kisseePos = targetPos + direction * kisserDistance;

        Location kisseeLocation = Location.Create(targetLocation.Area, kisseePos, kisseeFacing);

        kissee.ActionJumpToLocation(kisseeLocation);
        kisser.ActionJumpToLocation(targetLocation);

        Task.Run(async () =>
        {
            await Task.Delay(2000);
            await NwTask.SwitchToMainThread();

            await kissee.PlayAnimation(kisseeAnim, 1.0f, true, TimeSpan.FromHours(2.77));
            await kisser.PlayAnimation(kisserAnim, 1.0f, true, TimeSpan.FromHours(2.77));
        }).ContinueWith(t => { if (t.IsFaulted && t.Exception != null) throw t.Exception; });
    }

    private void PerformWaltz(NwCreature user, NwCreature target)
    {
        TimeSpan ghostDuration = TimeSpan.FromSeconds(5);
        Effect ghost = Effect.CutsceneGhost();
        user.ApplyEffect(EffectDuration.Temporary, ghost, ghostDuration);
        target.ApplyEffect(EffectDuration.Temporary, ghost, ghostDuration);

        Location? targetLocation = target.Location;
        if (targetLocation == null) return;

        float targetFacing = targetLocation.Rotation;
        float userFacing = GetOppositeDirection(targetFacing);
        float distance = 0.3f;

        System.Numerics.Vector3 targetPos = targetLocation.Position;
        System.Numerics.Vector3 direction = new System.Numerics.Vector3(
            (float)Math.Cos(targetFacing * Math.PI / 180.0f),
            (float)Math.Sin(targetFacing * Math.PI / 180.0f),
            0f
        );
        System.Numerics.Vector3 userPos = targetPos + direction * distance;
        Location userLocation = Location.Create(targetLocation.Area, userPos, userFacing);

        user.ActionJumpToLocation(userLocation);

        Task.Run(async () =>
        {
            await Task.Delay(3000);
            await NwTask.SwitchToMainThread();

            await user.PlayAnimation(Animation.LoopingCustom20, 1.0f, true, TimeSpan.FromHours(2.77));
            await target.PlayAnimation(Animation.LoopingCustom20, 1.0f, true, TimeSpan.FromHours(2.77));

            // Make user uncontrollable briefly for camera effect
            await Task.Delay(300);
            await NwTask.SwitchToMainThread();
            user.Commandable = false;

            await Task.Delay(1700);
            await NwTask.SwitchToMainThread();
            user.Commandable = true;
        }).ContinueWith(t => { if (t.IsFaulted && t.Exception != null) throw t.Exception; });
    }

    private void PerformLapSit(NwCreature user, NwCreature target, bool facing)
    {
        // Target sits first
        target.PlayAnimation(Animation.LoopingSitCross, 1.0f, true, TimeSpan.FromHours(2.77));

        TimeSpan ghostDuration = TimeSpan.FromSeconds(15);
        Effect ghost = Effect.CutsceneGhost();
        user.ApplyEffect(EffectDuration.Temporary, ghost, ghostDuration);
        target.ApplyEffect(EffectDuration.Temporary, ghost, ghostDuration);

        Location? targetLocation = target.Location;
        if (targetLocation == null) return;

        float targetFacing = targetLocation.Rotation;
        float userFacing;
        System.Numerics.Vector3 targetPos = targetLocation.Position;
        System.Numerics.Vector3 userPos;

        if (facing) // Facing target
        {
            userFacing = GetOppositeDirection(targetFacing);
            targetFacing = NormalizeDirection(targetFacing + 20.0f);
            userFacing = NormalizeDirection(userFacing - 10.0f);

            System.Numerics.Vector3 direction = new System.Numerics.Vector3(
                (float)Math.Cos(targetFacing * Math.PI / 180.0f),
                (float)Math.Sin(targetFacing * Math.PI / 180.0f),
                0f
            );
            userPos = targetPos - direction * 0.25f;
        }
        else // Facing away
        {
            userFacing = targetFacing;
            System.Numerics.Vector3 direction = new System.Numerics.Vector3(
                (float)Math.Cos(targetFacing * Math.PI / 180.0f),
                (float)Math.Sin(targetFacing * Math.PI / 180.0f),
                0f
            );
            userPos = targetPos + direction * 0.25f;
        }

        Location userLocation = Location.Create(targetLocation.Area, userPos, userFacing);
        user.ActionJumpToLocation(userLocation);

        Task.Run(async () =>
        {
            await Task.Delay(1000);
            await NwTask.SwitchToMainThread();
            await user.PlayAnimation(Animation.LoopingSitCross, 1.0f, true, TimeSpan.FromHours(2.77));
        }).ContinueWith(t => { if (t.IsFaulted && t.Exception != null) throw t.Exception; });
    }

    private void PerformGetCloseTo(NwCreature user, NwCreature target, bool fromBehind)
    {
        TimeSpan ghostDuration = TimeSpan.FromSeconds(15);
        Effect ghost = Effect.CutsceneGhost();
        user.ApplyEffect(EffectDuration.Temporary, ghost, ghostDuration);
        target.ApplyEffect(EffectDuration.Temporary, ghost, ghostDuration);

        Location? targetLocation = target.Location;
        if (targetLocation == null) return;

        float targetFacing;
        float userFacing;
        float vectorMod;

        if (fromBehind) // Hug from behind
        {
            targetFacing = GetOppositeDirection(target.Rotation);
            userFacing = target.Rotation;
            vectorMod = 0.25f;
        }
        else // Back close
        {
            targetFacing = target.Rotation;
            userFacing = targetFacing;
            vectorMod = 0.30f;
        }

        System.Numerics.Vector3 targetPos = targetLocation.Position;
        System.Numerics.Vector3 direction = new System.Numerics.Vector3(
            (float)Math.Cos(targetFacing * Math.PI / 180.0f),
            (float)Math.Sin(targetFacing * Math.PI / 180.0f),
            0f
        );
        System.Numerics.Vector3 userPos = targetPos + direction * vectorMod;
        Location userLocation = Location.Create(targetLocation.Area, userPos, userFacing);

        user.ActionJumpToLocation(userLocation);
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
                // Sit in chair animation
                target.PlayAnimation(Animation.LoopingSitChair, 1.0f, true, loopDuration);
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
                // Smoke pipe
                PerformSmokePipe(target);
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

    private void PerformSmokePipe(NwCreature target)
    {
        // Calculate height and distance based on race and gender (from dmfi script)
        float height = 1.7f;
        float distance = 0.1f;

        if (target.Gender == Gender.Male)
        {
            switch (target.Race.RacialType)
            {
                case RacialType.Human:
                case RacialType.HalfElf:
                    height = 1.7f;
                    distance = 0.12f;
                    break;
                case RacialType.Elf:
                    height = 1.55f;
                    distance = 0.08f;
                    break;
                case RacialType.Gnome:
                case RacialType.Halfling:
                    height = 1.15f;
                    distance = 0.12f;
                    break;
                case RacialType.Dwarf:
                    height = 1.2f;
                    distance = 0.12f;
                    break;
                case RacialType.HalfOrc:
                    height = 1.9f;
                    distance = 0.2f;
                    break;
            }
        }
        else // Female
        {
            switch (target.Race.RacialType)
            {
                case RacialType.Human:
                case RacialType.HalfElf:
                    height = 1.6f;
                    distance = 0.12f;
                    break;
                case RacialType.Elf:
                    height = 1.45f;
                    distance = 0.12f;
                    break;
                case RacialType.Gnome:
                case RacialType.Halfling:
                    height = 1.1f;
                    distance = 0.075f;
                    break;
                case RacialType.Dwarf:
                    height = 1.2f;
                    distance = 0.1f;
                    break;
                case RacialType.HalfOrc:
                    height = 1.8f;
                    distance = 0.13f;
                    break;
            }
        }

        Location? targetLoc = target.Location;
        if (targetLoc == null) return;

        // Calculate location above and in front of character's head
        System.Numerics.Vector3 position = targetLoc.Position;
        position.Z += height;

        float facing = targetLoc.Rotation;
        System.Numerics.Vector3 forward = new System.Numerics.Vector3(
            (float)Math.Cos(facing) * -distance,
            (float)Math.Sin(facing) * -distance,
            0f
        );

        position += forward;
        Location smokeLocation = Location.Create(targetLoc.Area, position, facing);

        // Apply red glow effect briefly
        Effect redGlow = Effect.VisualEffect(VfxType.DurLightRed5);
        target.ApplyEffect(EffectDuration.Temporary, redGlow, TimeSpan.FromSeconds(0.15));

        // Schedule smoke puff after a delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(3000);
            await NwTask.SwitchToMainThread();

            // Apply smoke puff VFX at the calculated location
            Effect smokePuff = Effect.VisualEffect(VfxType.FnfSmokePuff);
            smokeLocation.ApplyEffect(EffectDuration.Instant, smokePuff);

            // If female and not dwarf, turn head to left
            if (target.Gender == Gender.Female && target.Race.RacialType != RacialType.Dwarf)
            {
                await target.PlayAnimation(Animation.FireForgetHeadTurnLeft, 1.0f);
            }
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

    private void HandleTransformButton()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.", ColorConstants.Red);
            return;
        }

        // Get WindowDirector from AnvilCore service locator
        WindowDirector? windowDirector = AnvilCore.GetService<WindowDirector>();
        if (windowDirector == null)
        {
            _player.SendServerMessage("Failed to open transform window. Please report this bug.", ColorConstants.Red);
            return;
        }

        // Check if Transform window is already open - if so, close it (toggle behavior)
        if (windowDirector.IsWindowOpen(_player, typeof(EmoteTransformPresenter)))
        {
            windowDirector.CloseWindow(_player, typeof(EmoteTransformPresenter));
            return;
        }

        // Determine the target creature - use selected target if it's an associate or bottled companion
        NwCreature targetCreature = creature;
        if (Model.SelectedTarget != null && Model.SelectedTarget is NwCreature selectedCreature)
        {
            // Check if target is an associate of the player OR a bottled companion
            if (selectedCreature.Master == creature || IsBottledCompanion(selectedCreature))
            {
                targetCreature = selectedCreature;
            }
        }

        // Create the Transform window with the correct target
        EmoteTransformView transformView = new();
        EmoteTransformPresenter transformPresenter = new(transformView, _player, targetCreature);
        windowDirector.OpenWindow(transformPresenter);
    }

    public override void Close()
    {
        // Reset X and Y to 0, restore saved Z before closing
        NwCreature? creature = _player.LoginCreature;
        if (creature != null)
        {
            string pcKey = creature.GetObjectVariable<LocalVariableString>("pc_key").Value ?? "";
            float savedZ = creature.GetObjectVariable<LocalVariableFloat>($"{pcKey}_emote_saved_z").Value;

            creature.VisualTransform.Translation = new System.Numerics.Vector3(0f, 0f, savedZ);
        }

        _token.Close();
    }
}
