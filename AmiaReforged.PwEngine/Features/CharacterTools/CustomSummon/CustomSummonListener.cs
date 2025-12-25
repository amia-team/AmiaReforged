using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.CharacterTools.CustomSummon;

[ServiceBinding(typeof(CustomSummonListener))]
public class CustomSummonListener
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly WindowDirector _windowDirector;
    private readonly SchedulerService _schedulerService;

    public CustomSummonListener(WindowDirector windowDirector, SchedulerService schedulerService)
    {
        _windowDirector = windowDirector;
        _schedulerService = schedulerService;
        Log.Info("CustomSummonListener initialized.");
    }

    [ScriptHandler("i_custsummon")]
    public void OnItemActivated(CallInfo callInfo)
    {
        Log.Info("Custom Summon item script handler triggered");

        uint pcObject = NWScript.GetItemActivator();
        NwCreature? creature = pcObject.ToNwObject<NwCreature>();

        if (creature == null || !creature.IsValid)
        {
            Log.Warn("Creature is null or invalid");
            return;
        }

        NwPlayer? player = creature.ControllingPlayer;
        if (player == null)
        {
            Log.Warn("Player is null");
            return;
        }

        uint targetObject = NWScript.GetItemActivatedTarget();
        NwCreature? targetCreature = targetObject.ToNwObject<NwCreature>();
        uint widgetObject = NWScript.GetItemActivated();
        NwItem? widget = widgetObject.ToNwObject<NwItem>();

        if (widget == null || !widget.IsValid)
        {
            Log.Warn("Widget item is null or invalid");
            return;
        }

        // DM Mode - Adding summons to the widget
        if (player.IsDM)
        {
            HandleDmMode(player, widget, targetCreature);
            return;
        }

        // Player used item on themselves - open selection window
        if (targetObject == pcObject)
        {
            OpenSummonSelectionWindow(player, widget);
            return;
        }

        // Player is summoning
        HandlePlayerSummon(player, widget, targetObject);
    }

    private void HandleDmMode(NwPlayer player, NwItem widget, NwCreature? targetCreature)
    {
        // If DM used widget on themselves, open the DM configuration window
        if (targetCreature == player.ControlledCreature)
        {
            OpenDmConfigWindow(player, widget);
            return;
        }

        if (targetCreature == null || !targetCreature.IsValid)
        {
            player.SendServerMessage("Use this item on yourself to open the configuration window, or target a cust_summon template to add it.", ColorConstants.Orange);
            return;
        }

        if (targetCreature.Tag != "cust_summon")
        {
            player.SendServerMessage("You must use one of the summon templates for this item. Select a creature with the correct template.", ColorConstants.Orange);
            return;
        }

        // Store the summon as JSON
        string summonJson = NWScript.JsonDump(NWScript.ObjectToJson(targetCreature, 1));
        string summonName = targetCreature.Name;

        // Get current summon list
        List<string> summonNames = GetSummonNames(widget);
        List<string> summonJsons = GetSummonJsons(widget);

        // If this is the first summon, we'll need to add the item property
        bool isFirstSummon = summonNames.Count == 0;

        // Add new summon
        summonNames.Add(summonName);
        summonJsons.Add(summonJson);

        // Save back to widget
        SetSummonNames(widget, summonNames);
        SetSummonJsons(widget, summonJsons);

        // Update widget appearance
        UpdateWidgetAppearance(widget, summonNames);

        // If this was the first summon, add the cast spell property
        if (isFirstSummon)
        {
            AddWidgetItemProperty(widget);
            Log.Info("Added cast spell item property to widget (first summon via direct targeting)");
        }

        player.SendServerMessage($"Added summon: {summonName.ColorString(ColorConstants.Cyan)}", ColorConstants.Green);
        Log.Info($"DM {player.PlayerName} added summon '{summonName}' to widget");
    }

    private void AddWidgetItemProperty(NwItem widget)
    {
        // Remove any existing cast spell properties
        foreach (var prop in widget.ItemProperties.ToList())
        {
            if (prop.Property.PropertyType == ItemPropertyType.CastSpell)
            {
                widget.RemoveItemProperty(prop);
            }
        }

        // Add the single use per day cast spell property (spell 329)
        ItemProperty singleUse = ItemProperty.CastSpell((IPCastSpell)329, IPCastSpellNumUses.SingleUse);
        widget.AddItemProperty(singleUse, EffectDuration.Permanent);
    }

    private void OpenDmConfigWindow(NwPlayer player, NwItem widget)
    {
        Log.Info($"Opening Custom Summon DM Config for DM: {player.PlayerName}");

        // Restore the widget's single use so the DM (or player) can use it after configuration
        RestoreWidgetUse(widget);

        CustomSummonDmView view = new CustomSummonDmView(player, widget);

        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector != null)
        {
            injector.Inject(view.Presenter);
        }

        _windowDirector.OpenWindow(view.Presenter);

        player.SendServerMessage("Opening Custom Summon Manager...", ColorConstants.Cyan);
    }

    private void OpenSummonSelectionWindow(NwPlayer player, NwItem widget)
    {
        List<string> summonNames = GetSummonNames(widget);

        if (summonNames.Count == 0)
        {
            player.SendServerMessage("This widget has no summons configured.", ColorConstants.Orange);
            return;
        }

        if (summonNames.Count == 1)
        {
            player.SendServerMessage("You only have one summon saved on this widget. Use on the ground to summon.", ColorConstants.Cyan);
            return;
        }

        // Restore the widget's single use (similar to old toggle behavior)
        // This allows them to summon after selecting
        RestoreWidgetUse(widget);

        Log.Info($"Opening Custom Summon Selection for player: {player.PlayerName}");

        CustomSummonView view = new CustomSummonView(player, widget);

        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector != null)
        {
            injector.Inject(view.Presenter);
        }

        _windowDirector.OpenWindow(view.Presenter);

        player.SendServerMessage("Opening Summon Selection...", ColorConstants.Cyan);
    }

    private void HandlePlayerSummon(NwPlayer player, NwItem widget, uint targetLocation)
    {
        // Check if player already has a summon
        if (player.ControlledCreature?.Associates.Any(a => a.Tag == "cust_summon") == true)
        {
            player.SendServerMessage("You cannot have this summon alongside another summon. Unsummon before you try to call on this one.", ColorConstants.Orange);
            return;
        }

        List<string> summonJsons = GetSummonJsons(widget);
        int selectedIndex = widget.GetObjectVariable<LocalVariableInt>("summon_choice").Value;

        if (selectedIndex < 0 || selectedIndex >= summonJsons.Count)
        {
            selectedIndex = 0;
            widget.GetObjectVariable<LocalVariableInt>("summon_choice").Value = selectedIndex;
        }

        if (summonJsons.Count == 0)
        {
            player.SendServerMessage("This widget has no summons configured.", ColorConstants.Orange);
            return;
        }

        // Get target location
        Location? targetLoc = NWScript.GetItemActivatedTargetLocation();
        if (targetLoc == null)
        {
            player.SendServerMessage("Invalid target location.", ColorConstants.Orange);
            return;
        }

        // Calculate summon duration (1 hour per total character level)
        int pcLevel = player.ControlledCreature?.Level ?? 1;
        float summonDuration = 3600.0f * pcLevel;

        // Spawn the summon
        string summonJson = summonJsons[selectedIndex];
        NwCreature? summon = Json.Parse(summonJson).ToNwObject<NwCreature>(targetLoc);

        if (summon == null || !summon.IsValid)
        {
            player.SendServerMessage("Failed to summon creature.", ColorConstants.Red);
            return;
        }

        // Add as henchman
        NWScript.AddHenchman(player.ControlledCreature!, summon);

        // Apply ghostly effect
        Effect ghostEffect = Effect.CutsceneGhost();
        summon.ApplyEffect(EffectDuration.Permanent, ghostEffect);

        // Apply spawn VFX
        Effect spawnVfx = Effect.VisualEffect(VfxType.FnfSummonMonster1);
        summon.ApplyEffect(EffectDuration.Instant, spawnVfx);

        player.SendServerMessage($"Summon Duration: {summonDuration:F0} seconds.", ColorConstants.Cyan);

        // Schedule unsummon
        _schedulerService.Schedule(() => RemoveSummon(player, summon), TimeSpan.FromSeconds(summonDuration));
    }

    private void RemoveSummon(NwPlayer player, NwCreature summon)
    {
        if (summon.IsValid)
        {
            Effect unsummonVfx = Effect.VisualEffect(VfxType.ImpUnsummon);
            summon.ApplyEffect(EffectDuration.Instant, unsummonVfx);
            NWScript.RemoveHenchman(player.ControlledCreature!, summon);
            summon.Destroy();
            player.SendServerMessage("Your summon has returned to its realm.", ColorConstants.Cyan);
        }
    }

    private List<string> GetSummonNames(NwItem widget)
    {
        Log.Debug("GetSummonNames called");

        // Try new format first
        string? namesJson = widget.GetObjectVariable<LocalVariableString>("summon_names_json").Value;
        Log.Debug($"  - summon_names_json: {(string.IsNullOrEmpty(namesJson) ? "NULL" : namesJson)}");

        if (!string.IsNullOrEmpty(namesJson))
        {
            try
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<List<string>>(namesJson) ?? new List<string>();
                Log.Debug($"  - Deserialized {result.Count} names from new format");
                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"  - Failed to deserialize summon_names_json: {ex.Message}");
                // Fall through to old format check
            }
        }

        // Check for old format and migrate if found
        Log.Debug("  - Checking for old format data...");
        bool migrated = MigrateOldFormatIfNeeded(widget);
        if (migrated)
        {
            Log.Debug("  - Migration succeeded, retrying GetSummonNames");
            return GetSummonNames(widget);
        }

        Log.Debug("  - No data found, returning empty list");
        return new List<string>();
    }

    private List<string> GetSummonJsons(NwItem widget)
    {
        Log.Debug("GetSummonJsons called");

        // Try new format first
        string? jsonsJson = widget.GetObjectVariable<LocalVariableString>("summon_jsons_json").Value;
        Log.Debug($"  - summon_jsons_json: {(string.IsNullOrEmpty(jsonsJson) ? "NULL" : jsonsJson)}");

        if (!string.IsNullOrEmpty(jsonsJson))
        {
            try
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<List<string>>(jsonsJson) ?? new List<string>();
                Log.Debug($"  - Deserialized {result.Count} jsons from new format");
                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"  - Failed to deserialize summon_jsons_json: {ex.Message}");
                // Fall through to old format check
            }
        }

        // Check for old format and migrate if found
        Log.Debug("  - Checking for old format data...");
        bool migrated = MigrateOldFormatIfNeeded(widget);
        if (migrated)
        {
            Log.Debug("  - Migration succeeded, retrying GetSummonJsons");
            return GetSummonJsons(widget);
        }

        Log.Debug("  - No data found, returning empty list");
        return new List<string>();
    }

    private bool MigrateOldFormatIfNeeded(NwItem widget)
    {
        List<string> names = new List<string>();
        List<string> jsons = new List<string>();

        // Check if old format variables exist by looking for the first summon
        string? name1 = widget.GetObjectVariable<LocalVariableString>("summonName").Value;

        // Old NWScript used SetLocalJson which stores json data
        // We need to use JsonDump to convert the json type to a string
        var json1Obj = NWScript.GetLocalJson(widget, "summon_critter");
        string json1Str = NWScript.JsonDump(json1Obj);
        string? json1 = string.IsNullOrEmpty(json1Str) || json1Str == "{}" || json1Str == "null" ? null : json1Str;

        int customSet = widget.GetObjectVariable<LocalVariableInt>("custom_set").Value;
        int summonChoice = widget.GetObjectVariable<LocalVariableInt>("summonChoice").Value;

        Log.Info($"Checking widget for old format data:");
        Log.Info($"  - custom_set: {customSet}");
        Log.Info($"  - summonChoice: {summonChoice}");
        Log.Info($"  - summonName: {(string.IsNullOrEmpty(name1) ? "NULL" : name1)}");
        Log.Info($"  - summon_critter length: {(string.IsNullOrEmpty(json1) ? "0" : json1.Length.ToString())}");

        // If no old format data exists, return false
        if (string.IsNullOrEmpty(name1) && string.IsNullOrEmpty(json1))
        {
            Log.Debug("No old format data found for migration");
            return false; // Nothing to migrate
        }

        Log.Info($"Migrating old format widget data. Found summonName: {name1}");

        // Migrate summon 1
        if (!string.IsNullOrEmpty(name1) && !string.IsNullOrEmpty(json1))
        {
            names.Add(name1);
            jsons.Add(json1);
        }

        // Migrate summon 2 (check if it exists)
        string? name2 = widget.GetObjectVariable<LocalVariableString>("summonName2").Value;
        var json2Obj = NWScript.GetLocalJson(widget, "summon_critter2");
        string json2Str = NWScript.JsonDump(json2Obj);
        string? json2 = string.IsNullOrEmpty(json2Str) || json2Str == "{}" || json2Str == "null" ? null : json2Str;
        if (!string.IsNullOrEmpty(name2) && !string.IsNullOrEmpty(json2))
        {
            names.Add(name2);
            jsons.Add(json2);
        }

        // Migrate summon 3 (check if it exists)
        string? name3 = widget.GetObjectVariable<LocalVariableString>("summonName3").Value;
        var json3Obj = NWScript.GetLocalJson(widget, "summon_critter3");
        string json3Str = NWScript.JsonDump(json3Obj);
        string? json3 = string.IsNullOrEmpty(json3Str) || json3Str == "{}" || json3Str == "null" ? null : json3Str;
        if (!string.IsNullOrEmpty(name3) && !string.IsNullOrEmpty(json3))
        {
            names.Add(name3);
            jsons.Add(json3);
        }

        if (names.Count == 0)
            return false; // Nothing to migrate

        Log.Info($"Migrating {names.Count} summons from old format to new format");

        // Save to new format
        SetSummonNames(widget, names);
        SetSummonJsons(widget, jsons);

        // Migrate selection (old system was 1-based, new is 0-based)
        int oldChoice = widget.GetObjectVariable<LocalVariableInt>("summonChoice").Value;
        if (oldChoice > 0)
        {
            widget.GetObjectVariable<LocalVariableInt>("summon_choice").Value = oldChoice - 1;
        }

        // Clean up old variables
        widget.GetObjectVariable<LocalVariableInt>("custom_set").Delete();
        widget.GetObjectVariable<LocalVariableInt>("summonChoice").Delete();
        widget.GetObjectVariable<LocalVariableString>("summonName").Delete();
        widget.GetObjectVariable<LocalVariableString>("summonName2").Delete();
        widget.GetObjectVariable<LocalVariableString>("summonName3").Delete();
        NWScript.DeleteLocalJson(widget, "summon_critter");
        NWScript.DeleteLocalJson(widget, "summon_critter2");
        NWScript.DeleteLocalJson(widget, "summon_critter3");

        Log.Info($"Migration complete. Migrated {names.Count} summons successfully");

        return true;
    }

    private void SetSummonNames(NwItem widget, List<string> names)
    {
        string namesJson = System.Text.Json.JsonSerializer.Serialize(names);
        widget.GetObjectVariable<LocalVariableString>("summon_names_json").Value = namesJson;
    }

    private void SetSummonJsons(NwItem widget, List<string> jsons)
    {
        string jsonsJson = System.Text.Json.JsonSerializer.Serialize(jsons);
        widget.GetObjectVariable<LocalVariableString>("summon_jsons_json").Value = jsonsJson;
    }

    private void UpdateWidgetAppearance(NwItem widget, List<string> summonNames)
    {
        if (summonNames.Count == 0) return;

        string primaryName = summonNames[0];
        widget.Name = $"Summon <cÿÔ¦>{primaryName}</c>".ColorString(ColorConstants.Cyan);

        string description = $"This widget will summon {primaryName.ColorString(ColorConstants.Cyan)}.";

        if (summonNames.Count > 1)
        {
            description += " Use the widget on yourself to choose other stored summons through a NUI window.";
            for (int i = 1; i < summonNames.Count; i++)
            {
                description += $"\n\nSummon {i + 1}: {summonNames[i].ColorString(ColorConstants.Cyan)}";
            }
        }

        widget.Description = description;
    }

    private void RestoreWidgetUse(NwItem widget)
    {
        // Remove all existing cast spell properties
        foreach (var prop in widget.ItemProperties.ToList())
        {
            if (prop.Property.PropertyType == ItemPropertyType.CastSpell)
            {
                widget.RemoveItemProperty(prop);
            }
        }

        // Add back the single use per day cast spell property
        // Spell 329 = Identify, with 1 use per day (matches old NWScript)
        ItemProperty singleUse = ItemProperty.CastSpell((IPCastSpell)329, IPCastSpellNumUses.SingleUse);
        widget.AddItemProperty(singleUse, EffectDuration.Permanent);

        Log.Debug("Restored widget single use");
    }
}

