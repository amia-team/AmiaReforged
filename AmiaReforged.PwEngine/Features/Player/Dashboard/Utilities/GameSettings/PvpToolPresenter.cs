using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities.GameSettings;

public sealed class PvpToolPresenter : ScryPresenter<PvpToolView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private NwCreature? _selectedTarget;
    private bool _targetIsDead;
    private bool _targetIsPvpDeath;

    // Geometry bind to force window position
    private readonly NuiBind<NuiRect> _geometryBind = new("window_geometry");
    private static readonly NuiRect WindowPosition = new(360f, 100f, 320f, 450f);

    // PvP mode constants from inc_ds_died
    private const string DiedDeadMode = "pvp_dead_mode";
    private const string PvpRaiseBlock = "pvp_raise";
    private const string DiedPvpStorage = "ds_pvp_storage";

    // PvP mode values
    private const int DiedSubdualMode = 1;
    private const int DiedDuelMode = 2;
    private const int DiedBrawlMode = 3;

    public override PvpToolView View { get; }
    public override NuiWindowToken Token() => _token;

    public PvpToolPresenter(PvpToolView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), "PvP Tool (WIP)")
        {
            Geometry = _geometryBind,
            Resizable = true,
            Closable = true
        };
    }

    public override void Create()
    {
        if (_window is null)
        {
            _player.SendServerMessage("The PvP tool window could not be created.", ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        // Force the window position using the bind
        Token().SetBindValue(_geometryBind, WindowPosition);

        // Initialize PvP mode display
        UpdatePvpModeDisplay();

        Token().SetBindValue(View.TargetName, "No target selected");
        Token().SetBindValue(View.TargetStatus, "");
        Token().SetBindValue(View.ToggleButtonEnabled, false);
        Token().SetBindValue(View.ToggleButtonLabel, "Toggle Like/Dislike");
        Token().SetBindValue(View.RaiseButtonEnabled, false);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click) return;

        switch (ev.ElementId)
        {
            case "btn_select_target":
                HandleSelectTarget();
                break;
            case "btn_toggle":
                HandleTogglePvp();
                break;
            case "btn_raise":
                HandleRaisePvpVictim();
                break;
            case "btn_mode_subdual":
                SetPvpMode(DiedSubdualMode);
                break;
            case "btn_mode_duel":
                SetPvpMode(DiedDuelMode);
                break;
            case "btn_mode_brawl":
                SetPvpMode(DiedBrawlMode);
                break;
            case "btn_close":
                Close();
                break;
        }
    }

    private void SetPvpMode(int mode)
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.", ColorConstants.Red);
            return;
        }

        // Get the storage waypoint
        NwWaypoint? storage = NwObject.FindObjectsWithTag<NwWaypoint>(DiedPvpStorage).FirstOrDefault();
        if (storage == null)
        {
            _player.SendServerMessage("PvP storage waypoint not found. Contact a DM.", ColorConstants.Red);
            return;
        }

        // Get public CD key for storage
        string cdKey = NWScript.GetPCPublicCDKey(creature, NWScript.TRUE);

        // Set the mode
        storage.GetObjectVariable<LocalVariableInt>(cdKey).Value = mode;

        // Send feedback based on mode
        switch (mode)
        {
            case DiedSubdualMode:
                _player.SendServerMessage("PvP Mode set to Subdue [default]", ColorConstants.Cyan);
                break;
            case DiedDuelMode:
                _player.SendServerMessage("PvP Mode set to Duel", ColorConstants.Lime);
                break;
            case DiedBrawlMode:
                _player.SendServerMessage("PvP Mode set to Brawl", ColorConstants.Orange);
                break;
            default:
                _player.SendServerMessage("PvP Mode Disabled", ColorConstants.Gray);
                break;
        }

        UpdatePvpModeDisplay();
    }

    private void UpdatePvpModeDisplay()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null)
        {
            Token().SetBindValue(View.CurrentModeLabel, "Unknown");
            return;
        }

        // Get the storage waypoint
        NwWaypoint? storage = NwObject.FindObjectsWithTag<NwWaypoint>(DiedPvpStorage).FirstOrDefault();
        if (storage == null)
        {
            Token().SetBindValue(View.CurrentModeLabel, "Unknown");
            return;
        }

        // Get public CD key for storage
        string cdKey = NWScript.GetPCPublicCDKey(creature, NWScript.TRUE);
        int currentMode = storage.GetObjectVariable<LocalVariableInt>(cdKey).Value;

        string modeText = currentMode switch
        {
            DiedSubdualMode => "Subdue (Default)",
            DiedDuelMode => "Duel",
            DiedBrawlMode => "Brawl",
            _ => "None"
        };

        Token().SetBindValue(View.CurrentModeLabel, modeText);
    }

    private void HandleSelectTarget()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.", ColorConstants.Red);
            return;
        }

        _player.EnterTargetMode(OnTargetSelected);
        _player.SendServerMessage("Select a player, their associate, or a PvP corpse.", ColorConstants.Cyan);
    }

    private void OnTargetSelected(ModuleEvents.OnPlayerTarget targetEvent)
    {
        if (targetEvent.TargetObject == null || !targetEvent.TargetObject.IsValid)
        {
            _player.SendServerMessage("Invalid target selected.", ColorConstants.Orange);
            return;
        }

        if (targetEvent.TargetObject is not NwCreature targetCreature)
        {
            _player.SendServerMessage("You must target a creature.", ColorConstants.Orange);
            return;
        }

        NwCreature? myCreature = _player.LoginCreature;
        if (myCreature == null) return;

        // Reset state
        _targetIsDead = false;
        _targetIsPvpDeath = false;

        // Check if target is dead (potential PvP raise target)
        if (targetCreature.IsDead)
        {
            _targetIsDead = true;

            // Check if they died in PvP mode
            int pvpMode = targetCreature.GetObjectVariable<LocalVariableInt>(DiedDeadMode).Value;
            if (pvpMode > 0)
            {
                _targetIsPvpDeath = true;
                _selectedTarget = targetCreature;
                UpdateTargetDisplayForDead();
                return;
            }
            else
            {
                _player.SendServerMessage("This corpse is not recognized as being PvP-ed.", ColorConstants.Orange);
                return;
            }
        }

        // Get the actual player target (if targeting an associate, get their master)
        NwCreature actualTarget = targetCreature;
        if (targetCreature.Master != null && targetCreature.Master.IsPlayerControlled)
        {
            actualTarget = targetCreature.Master;
        }

        // Check if targeting self
        if (actualTarget == myCreature)
        {
            _player.SendServerMessage("You cannot target yourself!", ColorConstants.Orange);
            return;
        }

        // Check if target is a player
        if (!actualTarget.IsPlayerControlled)
        {
            _player.SendServerMessage("You can only target players or their associates!", ColorConstants.Orange);
            return;
        }

        // Check if in same party (compare faction leaders using NWScript)
        uint myLeader = NWScript.GetFactionLeader(myCreature);
        uint targetLeader = NWScript.GetFactionLeader(actualTarget);

        if (myLeader == targetLeader)
        {
            _player.SendServerMessage("You may only like or dislike PCs that aren't in your own party!", ColorConstants.Orange);
            return;
        }

        // Check if in No-PvP zone
        NwArea? area = myCreature.Area;
        if (area != null && area.GetObjectVariable<LocalVariableInt>("NoCasting").HasValue)
        {
            _player.SendServerMessage("You may not use the PvP Tool in a No-PvP or No-Casting Zone!", ColorConstants.Orange);
            return;
        }

        // Valid target - update UI
        _selectedTarget = actualTarget;
        UpdateTargetDisplay();
    }

    private void UpdateTargetDisplayForDead()
    {
        if (_selectedTarget == null || !_selectedTarget.IsValid)
        {
            Token().SetBindValue(View.TargetName, "No target selected");
            Token().SetBindValue(View.TargetStatus, "");
            Token().SetBindValue(View.ToggleButtonEnabled, false);
            Token().SetBindValue(View.RaiseButtonEnabled, false);
            return;
        }

        // Get target name
        string targetName = _selectedTarget.Name;
        Token().SetBindValue(View.TargetName, $"{targetName} (Dead)");
        Token().SetBindValue(View.TargetStatus, "PvP Death - Can be raised");

        // Hide like/dislike, show raise
        Token().SetBindValue(View.ToggleButtonEnabled, false);
        Token().SetBindValue(View.ToggleButtonLabel, "Toggle Like/Dislike");

        // Check raise cooldown
        bool canRaise = CanRaisePvpVictim(_selectedTarget);
        Token().SetBindValue(View.RaiseButtonEnabled, canRaise);
    }

    private void UpdateTargetDisplay()
    {
        if (_selectedTarget == null || !_selectedTarget.IsValid)
        {
            Token().SetBindValue(View.TargetName, "No target selected");
            Token().SetBindValue(View.TargetStatus, "");
            Token().SetBindValue(View.ToggleButtonEnabled, false);
            Token().SetBindValue(View.RaiseButtonEnabled, false);
            return;
        }

        NwCreature? myCreature = _player.LoginCreature;
        if (myCreature == null) return;

        // Get target name
        string targetName = _selectedTarget.Name;
        Token().SetBindValue(View.TargetName, targetName);

        // Check current relationship status using NWScript
        bool isEnemy = NWScript.GetIsEnemy(_selectedTarget, myCreature) == 1;

        if (isEnemy)
        {
            Token().SetBindValue(View.TargetStatus, "Currently: DISLIKED");
            Token().SetBindValue(View.ToggleButtonLabel, "Set to LIKED");
        }
        else
        {
            Token().SetBindValue(View.TargetStatus, "Currently: LIKED");
            Token().SetBindValue(View.ToggleButtonLabel, "Set to DISLIKED");
        }

        Token().SetBindValue(View.ToggleButtonEnabled, true);
        Token().SetBindValue(View.RaiseButtonEnabled, false);
    }

    private bool CanRaisePvpVictim(NwCreature target)
    {
        // Check if in free rest/free zone area
        NwArea? area = target.Area;
        if (area != null)
        {
            if (area.GetObjectVariable<LocalVariableInt>("FreeRest").Value == 1 ||
                area.GetObjectVariable<LocalVariableInt>("ds_freezone").Value == 1)
            {
                return true;
            }
        }

        // Check cooldown (5 minutes between raises)
        int blockTimeRemaining = GetBlockTimeRemaining(target, PvpRaiseBlock);
        return blockTimeRemaining <= 0;
    }

    private int GetBlockTimeRemaining(NwCreature creature, string blockVariable)
    {
        // Simple implementation - check if the block variable exists and has remaining time
        // The original uses GetIsBlocked which checks game time
        int blockHour = creature.GetObjectVariable<LocalVariableInt>($"{blockVariable}_hour").Value;
        int blockMinute = creature.GetObjectVariable<LocalVariableInt>($"{blockVariable}_minute").Value;

        if (blockHour == 0 && blockMinute == 0) return 0;

        // Get current game time
        int currentHour = NWScript.GetTimeHour();
        int currentMinute = NWScript.GetTimeMinute();

        // Calculate remaining time in minutes
        int elapsedMinutes = (currentHour - blockHour) * 60 + (currentMinute - blockMinute);
        int blockDuration = 5; // 5 minutes

        return Math.Max(0, blockDuration - elapsedMinutes);
    }

    private void SetBlockTime(NwCreature creature, string blockVariable, int minutes)
    {
        int currentHour = NWScript.GetTimeHour();
        int currentMinute = NWScript.GetTimeMinute();

        creature.GetObjectVariable<LocalVariableInt>($"{blockVariable}_hour").Value = currentHour;
        creature.GetObjectVariable<LocalVariableInt>($"{blockVariable}_minute").Value = currentMinute;
    }

    private void HandleRaisePvpVictim()
    {
        if (_selectedTarget == null || !_selectedTarget.IsValid || !_selectedTarget.IsDead)
        {
            _player.SendServerMessage("No valid PvP corpse selected.", ColorConstants.Orange);
            return;
        }

        if (!_targetIsPvpDeath)
        {
            _player.SendServerMessage("This corpse is not recognized as being PvP-ed.", ColorConstants.Orange);
            return;
        }

        if (!CanRaisePvpVictim(_selectedTarget))
        {
            _player.SendServerMessage("You can do a free raise on a PC once every 5 minutes!", ColorConstants.Orange);
            return;
        }

        // Perform resurrection
        Effect resEffect = Effect.Resurrection();
        _selectedTarget.ApplyEffect(EffectDuration.Instant, resEffect);

        // Remove dead status variables
        _selectedTarget.GetObjectVariable<LocalVariableInt>(DiedDeadMode).Delete();
        _selectedTarget.GetObjectVariable<LocalVariableInt>("is_dead").Delete();

        // Remove supernatural visual effects (from original script)
        foreach (Effect effect in _selectedTarget.ActiveEffects)
        {
            if (effect.SubType == EffectSubType.Supernatural &&
                effect.EffectType == EffectType.VisualEffect)
            {
                _selectedTarget.RemoveEffect(effect);
            }
        }

        // Set cooldown
        SetBlockTime(_selectedTarget, PvpRaiseBlock, 5);

        _player.SendServerMessage($"You have raised {_selectedTarget.Name} from PvP death.", ColorConstants.Green);

        // Reset UI
        _selectedTarget = null;
        _targetIsDead = false;
        _targetIsPvpDeath = false;
        Token().SetBindValue(View.TargetName, "No target selected");
        Token().SetBindValue(View.TargetStatus, "");
        Token().SetBindValue(View.RaiseButtonEnabled, false);
    }

    private void HandleTogglePvp()
    {
        if (_selectedTarget == null || !_selectedTarget.IsValid)
        {
            _player.SendServerMessage("No valid target selected.", ColorConstants.Orange);
            return;
        }

        NwCreature? myCreature = _player.LoginCreature;
        if (myCreature == null) return;

        // Check if in No-PvP zone
        NwArea? area = myCreature.Area;
        if (area != null && area.GetObjectVariable<LocalVariableInt>("NoCasting").HasValue)
        {
            _player.SendServerMessage("You may not use the PvP Tool in a No-PvP or No-Casting Zone!", ColorConstants.Orange);
            return;
        }

        // Toggle the relationship using NWScript
        bool isEnemy = NWScript.GetIsEnemy(_selectedTarget, myCreature) == 1;

        if (isEnemy)
        {
            // Currently disliked - set to liked
            NWScript.SetPCLike(myCreature, _selectedTarget);
            _player.SendServerMessage($"You now LIKE {_selectedTarget.Name}.", ColorConstants.Cyan);
        }
        else
        {
            // Currently liked - set to disliked
            NWScript.SetPCDislike(myCreature, _selectedTarget);
            _player.SendServerMessage($"You now DISLIKE {_selectedTarget.Name}.", ColorConstants.Orange);
        }

        UpdateTargetDisplay();
    }

    public override void UpdateView()
    {
        // No dynamic updates needed
    }

    public override void Close()
    {
        _selectedTarget = null;
        _targetIsDead = false;
        _targetIsPvpDeath = false;
        // Don't call RaiseCloseEvent() here - it causes infinite recursion
        // The WindowDirector handles cleanup when CloseWindow() is called
        _token.Close();
    }
}
