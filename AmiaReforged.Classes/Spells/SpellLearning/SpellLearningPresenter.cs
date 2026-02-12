using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NWN.Core.NWNX;
using NLog;

namespace AmiaReforged.Classes.Spells.SpellLearning;

public sealed class SpellLearningPresenter : ScryPresenter<SpellLearningView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public override SpellLearningView View { get; }
    private NuiWindowToken _token;
    private NuiWindow? _window;

    private readonly NwPlayer _player;
    private readonly ClassType _baseClass;
    private readonly int _effectiveCasterLevel;
    private readonly Dictionary<int, int> _spellsNeeded;

    // UI state
    private int _currentSpellLevel;
    private readonly Dictionary<int, List<SpellListRow>> _allSpells = new(); // All spells grouped by level
    private readonly List<SpellListRow> _visibleSpellListRows = new(); // Currently visible spells
    private readonly Dictionary<int, int> _spellsToLearnPerLevel = new();
    private readonly Dictionary<int, int> _selectedCountPerLevel = new();
    private readonly HashSet<int> _selectedSpellIds = new();
    private readonly HashSet<int> _knownSpellIds = new();

    public SpellLearningPresenter(SpellLearningView view, NwPlayer player, ClassType baseClass, int effectiveCasterLevel, Dictionary<int, int> spellsNeeded)
    {
        View = view;
        _player = player;
        _baseClass = baseClass;
        _effectiveCasterLevel = effectiveCasterLevel;
        _spellsNeeded = spellsNeeded;
    }

    public override NuiWindowToken Token() => _token;

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType == NuiEventType.Click)
        {
            HandleButtonClick(eventData);
        }
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        Log.Debug($"Button clicked: {eventData.ElementId}, ArrayIndex: {eventData.ArrayIndex}");

        if (eventData.ElementId == "confirm_button")
        {
            ConfirmSelections();
        }
        else if (eventData.ElementId == "cancel_button")
        {
            Cancel();
        }
        else if (eventData.ElementId.StartsWith("spell_level_button_"))
        {
            // Extract spell level from button ID (spell_level_button_0, spell_level_button_1, etc.)
            string levelStr = eventData.ElementId.Replace("spell_level_button_", "");
            if (int.TryParse(levelStr, out int spellLevel))
            {
                // Check if this spell level is available
                if (_spellsToLearnPerLevel.ContainsKey(spellLevel))
                {
                    _currentSpellLevel = spellLevel;
                    Log.Info($"Switched to spell level {_currentSpellLevel}");
                    RefreshSpellList();
                }
                else
                {
                    Log.Debug($"Spell level {spellLevel} not available for learning");
                }
            }
        }
        else if (eventData.ElementId == "spell_button")
        {
            // Use array index to identify which spell was clicked
            int rowIndex = eventData.ArrayIndex;
            if (rowIndex >= 0 && rowIndex < _visibleSpellListRows.Count)
            {
                SpellListRow row = _visibleSpellListRows[rowIndex];
                HandleSpellToggle(row);
            }
        }
    }

    private void HandleSpellToggle(SpellListRow row)
    {
        if (row.AlreadyKnown)
            return;

        int spellId = row.SpellId;
        int spellLevel = row.SpellLevel;
        int maxSelections = _spellsToLearnPerLevel.GetValueOrDefault(spellLevel, 0);
        int currentSelections = _selectedCountPerLevel.GetValueOrDefault(spellLevel, 0);

        // Toggle selection
        if (_selectedSpellIds.Contains(spellId))
        {
            _selectedSpellIds.Remove(spellId);
            _selectedCountPerLevel[spellLevel] = currentSelections - 1;
            row.IsSelected = false;
            Log.Info($"Deselected spell {spellId} (Level {spellLevel})");
        }
        else
        {
            if (currentSelections >= maxSelections)
            {
                _player.SendServerMessage($"You can only select {maxSelections} spell(s) of level {spellLevel}.", ColorConstants.Orange);
                return;
            }

            _selectedSpellIds.Add(spellId);
            _selectedCountPerLevel[spellLevel] = currentSelections + 1;
            row.IsSelected = true;
            Log.Info($"Selected spell {spellId} (Level {spellLevel})");
        }

        // Update UI
        UpdateSpellListBindings();
    }

    private void ConfirmSelections()
    {
        if (_player.ControlledCreature == null)
            return;

        NwCreature creature = _player.ControlledCreature;
        CreatureClassInfo? classInfo = creature.GetClassInfo(_baseClass);
        if (classInfo == null)
        {
            Log.Error($"Could not find class info for {_baseClass} on {creature.Name}");
            return;
        }

        int classId = classInfo.Class.Id;
        int currentLevel = creature.Level;

        // Get the ds_pckey item for persistent storage
        NwItem? pcKey = creature.Inventory.Items.FirstOrDefault(item => item.ResRef == "ds_pckey");
        if (pcKey == null)
        {
            Log.Error($"Could not find ds_pckey item for {creature.Name}");
            _player.SendServerMessage("Error: Could not save spell selections. Please report this to a DM.", ColorConstants.Red);
            return;
        }

        // Add all selected spells using NWNX (search across all spell levels)
        foreach (int spellId in _selectedSpellIds)
        {
            SpellListRow? row = _allSpells.Values
                .SelectMany(list => list)
                .FirstOrDefault(r => r.SpellId == spellId);

            if (row == null)
                continue;

            Log.Info($"Adding spell {spellId} (Level {row.SpellLevel}) to {creature.Name}'s {_baseClass} spellbook");
            CreaturePlugin.AddKnownSpell(creature, classId, row.SpellLevel, spellId);

            // Track this spell for delevel handling - store the level at which it was learned on ds_pckey
            string persistentKey = $"PRESTIGE_SPELL_{_baseClass}_{row.SpellLevel}_{spellId}";
            pcKey.GetObjectVariable<LocalVariableInt>(persistentKey).Value = currentLevel;
        }

        _player.SendServerMessage($"Successfully learned {_selectedSpellIds.Count} new spell(s)!", ColorConstants.Green);
        Log.Info($"{creature.Name} learned {_selectedSpellIds.Count} spells for {_baseClass} at character level {currentLevel}");

        Close();
    }

    private void Cancel()
    {
        _player.SendServerMessage("Spell learning cancelled.", ColorConstants.Orange);
        Close();
    }

    public override void InitBefore()
    {
        LoadKnownSpells();
        CalculateSpellsToLearn();
        BuildSpellList();

        string className = _baseClass == ClassType.Sorcerer ? "Sorcerer" : "Bard";
        string windowTitle = $"Learn {className} Spells";

        _window = new NuiWindow(View.RootLayout(), windowTitle)
        {
            Geometry = new NuiRect(150f, 30f, 720f, 650f)
        };
    }

    public override void Create()
    {
        InitBefore();

        if (_window == null)
        {
            Log.Error("Failed to create spell learning window");
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
        {
            Log.Error($"Failed to create NUI window for {_player.PlayerName}");
            return;
        }

        InitializeBindings();
    }

    public override void Close()
    {
        _token.Close();
    }

    private void LoadKnownSpells()
    {
        if (_player.ControlledCreature == null)
            return;

        CreatureClassInfo? classInfo = _player.ControlledCreature.GetClassInfo(_baseClass);
        if (classInfo == null)
            return;

        // Get all known spells across all levels
        for (int level = 0; level <= 9; level++)
        {
            IList<NwSpell>? knownSpells = classInfo.KnownSpells.ElementAtOrDefault(level);
            if (knownSpells != null)
            {
                foreach (NwSpell spell in knownSpells)
                {
                    _knownSpellIds.Add(spell.Id);
                }
            }
        }

        Log.Info($"Loaded {_knownSpellIds.Count} known spells for {_player.PlayerName}'s {_baseClass}");
    }

    private void CalculateSpellsToLearn()
    {
        _spellsToLearnPerLevel.Clear();

        // Use the spells needed that were calculated by the service
        foreach (var kvp in _spellsNeeded)
        {
            _spellsToLearnPerLevel[kvp.Key] = kvp.Value;
            _selectedCountPerLevel[kvp.Key] = 0;
            Log.Info($"Player can learn {kvp.Value} spell(s) of level {kvp.Key}");
        }
    }

    private void BuildSpellList()
    {
        _allSpells.Clear();

        // Build all spells grouped by level
        foreach (int spellLevel in _spellsToLearnPerLevel.Keys.OrderBy(k => k))
        {
            List<SpellListRow> spellsForLevel = new();

            // Get all spells of this level for the class
            List<NwSpell> availableSpells = GetAvailableSpellsForLevel(spellLevel);

            foreach (NwSpell spell in availableSpells.OrderBy(s => s.Name.ToString()))
            {
                bool alreadyKnown = _knownSpellIds.Contains(spell.Id);

                // Get icon ResRef - add fallback and log what we're getting
                string iconResRef = spell.IconResRef ?? "iit_spdispel";
                if (string.IsNullOrEmpty(iconResRef))
                {
                    iconResRef = "iit_spdispel";
                }

                // Log first spell's icon to debug
                if (spellsForLevel.Count == 0)
                {
                    Log.Debug($"First spell in level {spellLevel}: {spell.Name}, IconResRef: '{iconResRef}'");
                }

                spellsForLevel.Add(new SpellListRow
                {
                    SpellId = spell.Id,
                    SpellLevel = spellLevel,
                    SpellName = spell.Name.ToString(),
                    SpellIconResRef = iconResRef,
                    AlreadyKnown = alreadyKnown,
                    IsSelected = false
                });
            }

            _allSpells[spellLevel] = spellsForLevel;
        }

        // Set initial spell level to the first one with spells to learn
        _currentSpellLevel = _spellsToLearnPerLevel.Keys.OrderBy(k => k).First();

        int totalSpells = _allSpells.Values.Sum(list => list.Count);
        Log.Info($"Built spell list with {totalSpells} total spells across {_allSpells.Count} levels");

        // Don't call RefreshSpellList here - token doesn't exist yet!
        // It will be called from InitializeBindings() after the window is created
    }

    private void RefreshSpellList()
    {
        _visibleSpellListRows.Clear();

        if (_allSpells.TryGetValue(_currentSpellLevel, out List<SpellListRow>? spellsForLevel))
        {
            _visibleSpellListRows.AddRange(spellsForLevel);
        }


        // Update bindings (token is guaranteed to exist when this is called)
        UpdateHeaderAndList();
    }

    private List<NwSpell> GetAvailableSpellsForLevel(int spellLevel)
    {
        List<NwSpell> spells = new();

        // Get all spells from the NwSpell collection
        foreach (NwSpell spell in NwRuleset.Spells)
        {
            // Check if this spell is available to the base class at this level
            if (IsSpellAvailableForClass(spell, _baseClass, spellLevel))
            {
                spells.Add(spell);
            }
        }

        return spells;
    }

    private bool IsSpellAvailableForClass(NwSpell spell, ClassType classType, int spellLevel)
    {
        // Check if the spell is available at the correct level for this class
        try
        {
            NwClass? nwClass = NwClass.FromClassType(classType);
            if (nwClass == null)
                return false;

            // GetSpellLevelForClass returns 255 if the spell is not available for this class
#pragma warning disable CS0618 // Type or member is obsolete
            byte availableLevel = spell.GetSpellLevelForClass(nwClass);
#pragma warning restore CS0618 // Type or member is obsolete

            if (availableLevel != 255 && availableLevel == spellLevel)
                return true;
        }
        catch (Exception ex)
        {
            Log.Warn(ex, $"Error checking spell {spell.Name} availability for {classType}");
        }

        return false;
    }

    private void InitializeBindings()
    {
        string className = _baseClass == ClassType.Sorcerer ? "Sorcerer" : "Bard";

        _token.SetBindValue(View.HeaderText, $"Learn {className} Spells (Effective Caster Level {_effectiveCasterLevel})");
        _token.SetBindValue(View.InstructionText, "Select the spells you want to learn.");

        // Set up individual spell level buttons (0-9)
        SetupSpellLevelButton(0, View.SpellLevelButtonText0, View.SpellLevelButtonEnabled0);
        SetupSpellLevelButton(1, View.SpellLevelButtonText1, View.SpellLevelButtonEnabled1);
        SetupSpellLevelButton(2, View.SpellLevelButtonText2, View.SpellLevelButtonEnabled2);
        SetupSpellLevelButton(3, View.SpellLevelButtonText3, View.SpellLevelButtonEnabled3);
        SetupSpellLevelButton(4, View.SpellLevelButtonText4, View.SpellLevelButtonEnabled4);
        SetupSpellLevelButton(5, View.SpellLevelButtonText5, View.SpellLevelButtonEnabled5);
        SetupSpellLevelButton(6, View.SpellLevelButtonText6, View.SpellLevelButtonEnabled6);
        SetupSpellLevelButton(7, View.SpellLevelButtonText7, View.SpellLevelButtonEnabled7);
        SetupSpellLevelButton(8, View.SpellLevelButtonText8, View.SpellLevelButtonEnabled8);
        SetupSpellLevelButton(9, View.SpellLevelButtonText9, View.SpellLevelButtonEnabled9);

        // Now that token exists, populate the visible spell list
        RefreshSpellList();
    }

    private void SetupSpellLevelButton(int level, NuiBind<string> textBind, NuiBind<bool> enabledBind)
    {
        if (_spellsToLearnPerLevel.ContainsKey(level))
        {
            int max = _spellsToLearnPerLevel[level];
            int current = _selectedCountPerLevel.GetValueOrDefault(level, 0);
            _token.SetBindValue(textBind, $"{level} ({current}/{max})");
            _token.SetBindValue(enabledBind, true);
        }
        else
        {
            _token.SetBindValue(textBind, $"{level} --");
            _token.SetBindValue(enabledBind, false);
        }
    }

    private void UpdateHeaderAndList()
    {
        // Update individual spell level buttons with current counts
        UpdateSpellLevelButton(0, View.SpellLevelButtonText0);
        UpdateSpellLevelButton(1, View.SpellLevelButtonText1);
        UpdateSpellLevelButton(2, View.SpellLevelButtonText2);
        UpdateSpellLevelButton(3, View.SpellLevelButtonText3);
        UpdateSpellLevelButton(4, View.SpellLevelButtonText4);
        UpdateSpellLevelButton(5, View.SpellLevelButtonText5);
        UpdateSpellLevelButton(6, View.SpellLevelButtonText6);
        UpdateSpellLevelButton(7, View.SpellLevelButtonText7);
        UpdateSpellLevelButton(8, View.SpellLevelButtonText8);
        UpdateSpellLevelButton(9, View.SpellLevelButtonText9);

        // Build header text for current spell level
        int maxSpells = _spellsToLearnPerLevel.GetValueOrDefault(_currentSpellLevel, 0);
        int selectedSpells = _selectedCountPerLevel.GetValueOrDefault(_currentSpellLevel, 0);
        string headerText = $"Spell Level {_currentSpellLevel}: Select {maxSpells - selectedSpells} more spell(s).";
        _token.SetBindValue(View.SpellLevelHeaderText, headerText);

        // Update the visible spell list count
        _token.SetBindValue(View.SpellListCount, _visibleSpellListRows.Count);

        // Update spell buttons for visible spells only
        List<string> buttonTexts = new();
        List<string> tooltips = new();
        List<bool> enabledStates = new();
        List<string> statusTexts = new();
        // List<string> iconResRefs = new(); // Temporarily disabled while fixing layout

        foreach (SpellListRow row in _visibleSpellListRows)
        {
            string buttonText = row.SpellName;
            if (row.IsSelected)
                buttonText = "+ " + buttonText;

            buttonTexts.Add(buttonText);
            tooltips.Add(row.AlreadyKnown ? "Already Known" : $"Learn {row.SpellName}");
            enabledStates.Add(!row.AlreadyKnown);

            string statusText = row.AlreadyKnown ? "(Known)" : row.IsSelected ? "(Selected)" : "";
            statusTexts.Add(statusText);

            // iconResRefs.Add(row.SpellIconResRef); // Temporarily disabled
        }

        _token.SetBindValues(View.SpellButtonText, buttonTexts);
        _token.SetBindValues(View.SpellTooltip, tooltips);
        _token.SetBindValues(View.SpellEnabled, enabledStates);
        _token.SetBindValues(View.SpellStatusText, statusTexts);
        // _token.SetBindValues(View.SpellIconResRef, iconResRefs); // Temporarily disabled

        // Check if all required spells are selected
        bool allSelected = _spellsToLearnPerLevel.All(kvp =>
            _selectedCountPerLevel.GetValueOrDefault(kvp.Key, 0) == kvp.Value);

        _token.SetBindValue(View.CanConfirm, allSelected);
    }

    private void UpdateSpellLevelButton(int level, NuiBind<string> textBind)
    {
        if (_spellsToLearnPerLevel.ContainsKey(level))
        {
            int max = _spellsToLearnPerLevel[level];
            int current = _selectedCountPerLevel.GetValueOrDefault(level, 0);
            _token.SetBindValue(textBind, $"{level} ({current}/{max})");
        }
    }

    private void UpdateSpellListBindings()
    {
        UpdateHeaderAndList();
    }

    private class SpellListRow
    {
        public int SpellId { get; set; }
        public int SpellLevel { get; set; }
        public string SpellName { get; set; } = string.Empty;
        public string SpellIconResRef { get; set; } = string.Empty;
        public bool AlreadyKnown { get; set; }
        public bool IsSelected { get; set; }
    }
}

