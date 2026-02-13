using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.GuardSpawner;

/// <summary>
/// Model for the Guard Spawner tool. Handles state management and widget creation logic.
/// </summary>
public sealed class GuardSpawnerModel
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string CopyWaypointTag = "ds_copy";
    private const string GuardTag = "settle_stndguard";
    private const string NormalWidgetResRef = "pc_guards";
    private const string BeaconWidgetResRef = "pc_guards_bcn";
    private const int MaxCreatures = 4;
    private const int MaxQuantity = 8;

    // NWScript creature event script constants
    private const int EVENT_SCRIPT_CREATURE_ON_BLOCKED_BY_DOOR = 5000;
    private const int EVENT_SCRIPT_CREATURE_ON_DAMAGED = 5002;
    private const int EVENT_SCRIPT_CREATURE_ON_DEATH = 5003;
    private const int EVENT_SCRIPT_CREATURE_ON_DIALOGUE = 5004;
    private const int EVENT_SCRIPT_CREATURE_ON_DISTURBED = 5005;
    private const int EVENT_SCRIPT_CREATURE_ON_END_COMBATROUND = 5006;
    private const int EVENT_SCRIPT_CREATURE_ON_HEARTBEAT = 5001;
    private const int EVENT_SCRIPT_CREATURE_ON_MELEE_ATTACKED = 5008;
    private const int EVENT_SCRIPT_CREATURE_ON_NOTICE = 5007;
    private const int EVENT_SCRIPT_CREATURE_ON_RESTED = 5009;
    private const int EVENT_SCRIPT_CREATURE_ON_SPAWN_IN = 5010;
    private const int EVENT_SCRIPT_CREATURE_ON_SPELLCASTAT = 5011;
    private const int EVENT_SCRIPT_CREATURE_ON_USER_DEFINED_EVENT = 5012;

    private readonly NwPlayer _player;

    public GuardSpawnerModel(NwPlayer player)
    {
        _player = player;
    }

    // State
    public GuardSpawnerData.GuardSettlement? SelectedSettlement { get; private set; }
    public List<GuardSpawnerData.GuardCreature> ChosenCreatures { get; } = new();
    public int Quantity { get; set; } = 1;
    public string WidgetName { get; set; } = string.Empty;
    public bool IsBeaconMode { get; private set; }
    public NwItem? EditingWidget { get; private set; }

    // Events
    public delegate void ModelUpdatedHandler();
    public event ModelUpdatedHandler? OnModelUpdated;

    public delegate void WidgetLoadedHandler();
    public event WidgetLoadedHandler? OnWidgetLoaded;

    /// <summary>
    /// Sets the selected settlement and notifies listeners.
    /// </summary>
    public void SetSelectedSettlement(int index)
    {
        if (index < 0 || index >= GuardSpawnerData.AllSettlements.Count)
        {
            SelectedSettlement = null;
            return;
        }

        SelectedSettlement = GuardSpawnerData.AllSettlements[index];
        OnModelUpdated?.Invoke();
    }

    /// <summary>
    /// Adds a creature to the chosen list (max 4).
    /// </summary>
    public bool AddCreature(GuardSpawnerData.GuardCreature creature)
    {
        if (ChosenCreatures.Count >= MaxCreatures)
        {
            _player.SendServerMessage("Cannot add more than 4 guards to a widget.", ColorConstants.Orange);
            return false;
        }

        ChosenCreatures.Add(creature);
        OnModelUpdated?.Invoke();
        return true;
    }

    /// <summary>
    /// Adds a creature by index from the current settlement's creature list.
    /// </summary>
    public bool AddCreatureByIndex(int index)
    {
        if (SelectedSettlement == null)
        {
            _player.SendServerMessage("Please select a settlement first.", ColorConstants.Orange);
            return false;
        }

        var creatures = GuardSpawnerData.GetCreaturesForSettlement(SelectedSettlement.DisplayName);
        if (index < 0 || index >= creatures.Count)
        {
            return false;
        }

        return AddCreature(creatures[index]);
    }

    /// <summary>
    /// Removes a creature at the specified index.
    /// </summary>
    public void RemoveCreatureAt(int index)
    {
        if (index < 0 || index >= ChosenCreatures.Count) return;

        ChosenCreatures.RemoveAt(index);
        OnModelUpdated?.Invoke();
    }

    /// <summary>
    /// Toggles beacon mode on/off.
    /// </summary>
    public void ToggleBeacon()
    {
        IsBeaconMode = !IsBeaconMode;
        OnModelUpdated?.Invoke();
    }

    /// <summary>
    /// Resets all state to defaults.
    /// </summary>
    public void Reset()
    {
        SelectedSettlement = null;
        ChosenCreatures.Clear();
        Quantity = 1;
        WidgetName = string.Empty;
        IsBeaconMode = false;
        EditingWidget = null;
        OnModelUpdated?.Invoke();
    }

    /// <summary>
    /// Enters targeting mode to select an existing guard widget.
    /// </summary>
    public void EnterWidgetTargetMode()
    {
        _player.SendServerMessage("Select an existing guard widget (pc_guards or pc_guards_bcn).", ColorConstants.Cyan);
        _player.EnterTargetMode(OnWidgetTargeted, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Item
        });
    }

    private void OnWidgetTargeted(ModuleEvents.OnPlayerTarget targetEvent)
    {
        if (targetEvent.TargetObject is not NwItem item)
        {
            _player.SendServerMessage("Target must be an item.", ColorConstants.Orange);
            return;
        }

        string resRef = item.ResRef;
        if (resRef != NormalWidgetResRef && resRef != BeaconWidgetResRef)
        {
            _player.SendServerMessage($"Target must be a guard widget (pc_guards or pc_guards_bcn). Found: {resRef}", ColorConstants.Orange);
            return;
        }

        ParseExistingWidget(item);
    }

    /// <summary>
    /// Parses an existing widget and populates the model state.
    /// </summary>
    private void ParseExistingWidget(NwItem widget)
    {
        Reset();
        EditingWidget = widget;
        IsBeaconMode = widget.ResRef == BeaconWidgetResRef;

        // Read quantity
        int qty = widget.GetObjectVariable<LocalVariableInt>("qty").Value;
        Quantity = qty > 0 ? Math.Min(qty, MaxQuantity) : 1;

        // Read widget name from item name (format: "Summon {Settlement} {Name}")
        string itemName = widget.Name;
        if (itemName.StartsWith("Summon "))
        {
            string remainder = itemName.Substring(7); // Remove "Summon "
            if (IsBeaconMode && remainder.EndsWith(" (Beacon Settings)"))
            {
                remainder = remainder.Substring(0, remainder.Length - 18);
            }
            WidgetName = remainder;
        }

        // Read guard names for display (we can't fully reconstruct creatures from JSON easily,
        // but we can show what's stored)
        for (int i = 1; i <= MaxCreatures; i++)
        {
            Json guardJson = NWScript.GetLocalJson(widget, $"guard_critter{i}");
            string jsonStr = NWScript.JsonDump(guardJson);
            if (!string.IsNullOrEmpty(jsonStr) && jsonStr != "null" && jsonStr != "{}")
            {
                // Try to get the name from the guard variable
                string? guardName = widget.GetObjectVariable<LocalVariableString>($"guardName{(i == 1 ? "" : i.ToString())}").Value;
                if (!string.IsNullOrEmpty(guardName))
                {
                    // Create a placeholder creature entry for display
                    ChosenCreatures.Add(new GuardSpawnerData.GuardCreature(guardName, "unknown"));
                }
            }
        }

        _player.SendServerMessage($"Loaded widget: {widget.Name} with {ChosenCreatures.Count} guard(s).", ColorConstants.Lime);
        OnWidgetLoaded?.Invoke();
    }

    /// <summary>
    /// Validates the current state before building.
    /// </summary>
    public (bool IsValid, string ErrorMessage) Validate()
    {
        if (SelectedSettlement == null && EditingWidget == null)
        {
            return (false, "Please select a settlement.");
        }

        if (ChosenCreatures.Count == 0)
        {
            return (false, "Please add at least one guard creature.");
        }

        if (Quantity < 1 || Quantity > MaxQuantity)
        {
            return (false, $"Quantity must be between 1 and {MaxQuantity}.");
        }

        if (string.IsNullOrWhiteSpace(WidgetName))
        {
            return (false, "Please enter a widget name.");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Builds the guard widget with all configured settings.
    /// </summary>
    public async Task<bool> BuildWidget()
    {
        var (isValid, errorMessage) = Validate();
        if (!isValid)
        {
            _player.SendServerMessage(errorMessage, ColorConstants.Orange);
            return false;
        }

        NwCreature? dmCreature = _player.LoginCreature;
        if (dmCreature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.", ColorConstants.Red);
            return false;
        }

        // Find the copy waypoint
        NwWaypoint? copyWaypoint = NwObject.FindObjectsWithTag<NwWaypoint>(CopyWaypointTag).FirstOrDefault();
        if (copyWaypoint == null)
        {
            _player.SendServerMessage($"Error: Could not find waypoint '{CopyWaypointTag}'.", ColorConstants.Red);
            return false;
        }

        // If editing an existing widget, update it in place
        if (EditingWidget != null && EditingWidget.IsValid)
        {
            return await UpdateExistingWidget(EditingWidget, copyWaypoint);
        }

        // Otherwise, create a new widget
        return await CreateNewWidget(dmCreature, copyWaypoint);
    }

    /// <summary>
    /// Updates an existing widget in place without destroying and recreating it.
    /// </summary>
    private async Task<bool> UpdateExistingWidget(NwItem widget, NwWaypoint copyWaypoint)
    {
        // First, clear all existing guard JSONs and name variables
        for (int i = 1; i <= MaxCreatures; i++)
        {
            NWScript.DeleteLocalJson(widget, $"guard_critter{i}");
            string nameVar = i == 1 ? "guardName" : $"guardName{i}";
            widget.GetObjectVariable<LocalVariableString>(nameVar).Delete();
        }

        // Process each chosen creature
        int guardIndex = 1;
        string firstGuardName = string.Empty;

        foreach (var creatureData in ChosenCreatures)
        {
            // Skip placeholder entries from loaded widgets that haven't been replaced
            if (creatureData.ResRef == "unknown")
            {
                guardIndex++;
                continue;
            }

            NwCreature? creature = NwCreature.Create(creatureData.ResRef, copyWaypoint.Location!);
            if (creature == null)
            {
                Log.Warn($"Failed to create creature with resref: {creatureData.ResRef}");
                continue;
            }

            // Configure the creature
            ConfigureGuardCreature(creature, creatureData.ResRef);

            // Store first guard name
            if (guardIndex == 1)
            {
                firstGuardName = creature.Name;
            }

            // Serialize and store on widget
            Json creatureJson = NWScript.ObjectToJson(creature, 1);
            NWScript.SetLocalJson(widget, $"guard_critter{guardIndex}", creatureJson);

            // Store the name for reference
            string nameVar = guardIndex == 1 ? "guardName" : $"guardName{guardIndex}";
            widget.GetObjectVariable<LocalVariableString>(nameVar).Value = creature.Name;

            // Destroy the temporary creature
            creature.Destroy();

            guardIndex++;
        }

        // Update widget variables
        SetWidgetVariables(widget, firstGuardName, guardIndex - 1);

        // Update widget name
        string settlementName = SelectedSettlement?.DisplayName ?? "Guards";
        string finalName = $"Summon {settlementName} {WidgetName}";
        if (IsBeaconMode)
        {
            finalName += " (Beacon Settings)";
        }
        widget.Name = finalName;

        _player.SendServerMessage($"Successfully updated widget: {finalName}", ColorConstants.Lime);

        // Clear the editing reference
        EditingWidget = null;

        return true;
    }

    /// <summary>
    /// Creates a new guard widget from scratch.
    /// </summary>
    private async Task<bool> CreateNewWidget(NwCreature dmCreature, NwWaypoint copyWaypoint)
    {
        // Determine which resref to use
        string widgetResRef = IsBeaconMode ? BeaconWidgetResRef : NormalWidgetResRef;

        // Create the widget item
        NwItem? widget = await NwItem.Create(widgetResRef, dmCreature);
        if (widget == null)
        {
            _player.SendServerMessage("Error: Could not create widget item.", ColorConstants.Red);
            return false;
        }

        // Process each chosen creature
        int guardIndex = 1;
        string firstGuardName = string.Empty;

        foreach (var creatureData in ChosenCreatures)
        {
            // Skip placeholder entries from loaded widgets
            if (creatureData.ResRef == "unknown")
            {
                guardIndex++;
                continue;
            }

            NwCreature? creature = NwCreature.Create(creatureData.ResRef, copyWaypoint.Location!);
            if (creature == null)
            {
                Log.Warn($"Failed to create creature with resref: {creatureData.ResRef}");
                continue;
            }

            // Configure the creature
            ConfigureGuardCreature(creature, creatureData.ResRef);

            // Store first guard name
            if (guardIndex == 1)
            {
                firstGuardName = creature.Name;
            }

            // Serialize and store on widget
            Json creatureJson = NWScript.ObjectToJson(creature, 1);
            NWScript.SetLocalJson(widget, $"guard_critter{guardIndex}", creatureJson);

            // Store the name for reference
            string nameVar = guardIndex == 1 ? "guardName" : $"guardName{guardIndex}";
            widget.GetObjectVariable<LocalVariableString>(nameVar).Value = creature.Name;

            // Destroy the temporary creature
            creature.Destroy();

            guardIndex++;
        }

        // Set widget variables
        SetWidgetVariables(widget, firstGuardName, guardIndex - 1);

        // Set widget name and description
        string settlementName = SelectedSettlement?.DisplayName ?? "Guards";
        string finalName = $"Summon {settlementName} {WidgetName}";
        if (IsBeaconMode)
        {
            finalName += " (Beacon Settings)";
        }
        widget.Name = finalName;
        widget.Description = "This widget summons your guards. They can only be used in areas where you are authorized as a settlement leader.";

        _player.SendServerMessage($"Successfully created widget: {finalName}", ColorConstants.Lime);


        return true;
    }

    /// <summary>
    /// Configures a guard creature with proper tag, scripts, effects, and scale.
    /// </summary>
    private void ConfigureGuardCreature(NwCreature creature, string resRef)
    {
        // Set tag
        creature.Tag = GuardTag;

        // Clear conversation by setting dialogue event script to empty
        // (The conversation field itself is set in the blueprint, but we clear the dialogue script)
        NWScript.SetEventScript(creature, NWScript.EVENT_SCRIPT_CREATURE_ON_DIALOGUE, "");

        // Apply cutscene ghost if no_collision is set
        int noCollision = creature.GetObjectVariable<LocalVariableInt>("no_collision").Value;
        if (noCollision == 1)
        {
            creature.ApplyEffect(EffectDuration.Permanent, Effect.CutsceneGhost());
        }

        // Apply scale from variable (default 1.0)
        float scale = creature.GetObjectVariable<LocalVariableFloat>("scale").Value;
        if (scale <= 0f) scale = 1.0f;
        creature.VisualTransform.Scale = scale;

        // Apply AI scripts using NWScript
        var scripts = GuardSpawnerData.GetAiScriptsForCreature(resRef);
        foreach (var (eventType, scriptName) in scripts)
        {
            int nwScriptEvent = EventScriptTypeToNwScript(eventType);
            if (nwScriptEvent >= 0)
            {
                NWScript.SetEventScript(creature, nwScriptEvent, scriptName);
            }
        }
    }

    /// <summary>
    /// Converts an Anvil EventScriptType to the NWScript EVENT_SCRIPT constant.
    /// </summary>
    private static int EventScriptTypeToNwScript(EventScriptType eventType)
    {
        return eventType switch
        {
            EventScriptType.CreatureOnBlockedByDoor => EVENT_SCRIPT_CREATURE_ON_BLOCKED_BY_DOOR,
            EventScriptType.CreatureOnDamaged => EVENT_SCRIPT_CREATURE_ON_DAMAGED,
            EventScriptType.CreatureOnDeath => EVENT_SCRIPT_CREATURE_ON_DEATH,
            EventScriptType.CreatureOnDialogue => EVENT_SCRIPT_CREATURE_ON_DIALOGUE,
            EventScriptType.CreatureOnDisturbed => EVENT_SCRIPT_CREATURE_ON_DISTURBED,
            EventScriptType.CreatureOnEndCombatRound => EVENT_SCRIPT_CREATURE_ON_END_COMBATROUND,
            EventScriptType.CreatureOnHeartbeat => EVENT_SCRIPT_CREATURE_ON_HEARTBEAT,
            EventScriptType.CreatureOnMeleeAttacked => EVENT_SCRIPT_CREATURE_ON_MELEE_ATTACKED,
            EventScriptType.CreatureOnNotice => EVENT_SCRIPT_CREATURE_ON_NOTICE,
            EventScriptType.CreatureOnRested => EVENT_SCRIPT_CREATURE_ON_RESTED,
            EventScriptType.CreatureOnSpawnIn => EVENT_SCRIPT_CREATURE_ON_SPAWN_IN,
            EventScriptType.CreatureOnSpellCastAt => EVENT_SCRIPT_CREATURE_ON_SPELLCASTAT,
            EventScriptType.CreatureOnUserDefinedEvent => EVENT_SCRIPT_CREATURE_ON_USER_DEFINED_EVENT,
            _ => -1
        };
    }

    /// <summary>
    /// Sets the required variables on the widget item.
    /// </summary>
    private void SetWidgetVariables(NwItem widget, string firstGuardName, int guardCount)
    {
        // Set guard count
        widget.GetObjectVariable<LocalVariableInt>("guardCount").Value = guardCount;

        // Set first guard name
        widget.GetObjectVariable<LocalVariableString>("guardName").Value = firstGuardName;

        // Set quantity
        int qty = IsBeaconMode ? 4 : Quantity;
        widget.GetObjectVariable<LocalVariableInt>("qty").Value = qty;

        // Set settlement variables
        if (IsBeaconMode)
        {
            // Beacon mode: use all beacon alliance settlements
            widget.GetObjectVariable<LocalVariableInt>("ally_count").Value = GuardSpawnerData.BeaconAllianceSettlementIds.Length;

            for (int i = 0; i < GuardSpawnerData.BeaconAllianceSettlementIds.Length; i++)
            {
                widget.GetObjectVariable<LocalVariableInt>($"settlement_{i + 1}").Value =
                    GuardSpawnerData.BeaconAllianceSettlementIds[i];
            }
        }
        else if (SelectedSettlement != null)
        {
            // Normal mode: use selected settlement's linked IDs
            int[] linkedIds = SelectedSettlement.LinkedSettlementIds;
            widget.GetObjectVariable<LocalVariableInt>("ally_count").Value = linkedIds.Length;

            for (int i = 0; i < linkedIds.Length; i++)
            {
                widget.GetObjectVariable<LocalVariableInt>($"settlement_{i + 1}").Value = linkedIds[i];
            }
        }
    }

    /// <summary>
    /// Gets settlement options for the dropdown.
    /// </summary>
    public static List<NuiComboEntry> GetSettlementOptions()
    {
        List<NuiComboEntry> entries = new();
        for (int i = 0; i < GuardSpawnerData.AllSettlements.Count; i++)
        {
            entries.Add(new NuiComboEntry(GuardSpawnerData.AllSettlements[i].DisplayName, i));
        }
        return entries;
    }

    /// <summary>
    /// Gets creature options for the currently selected settlement.
    /// </summary>
    public List<NuiComboEntry> GetCreatureOptions()
    {
        if (SelectedSettlement == null)
        {
            return new List<NuiComboEntry> { new("(Select a settlement first)", 0) };
        }

        var creatures = GuardSpawnerData.GetCreaturesForSettlement(SelectedSettlement.DisplayName);
        List<NuiComboEntry> entries = new();
        for (int i = 0; i < creatures.Count; i++)
        {
            string label = creatures[i].DisplayName;
            if (GuardSpawnerData.IsMageCreature(creatures[i].ResRef))
            {
                label += " (Mage)";
            }
            entries.Add(new NuiComboEntry(label, i));
        }
        return entries;
    }
}



