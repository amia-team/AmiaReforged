using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NWN.Core;
using NWN.Core.NWNX;
using NLog;

namespace AmiaReforged.Classes.Spells.SpellLearning;

public sealed class SpellLearningPresenter : ScryPresenter<SpellLearningView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    // Cache the spells.2da table for spell descriptions
    private static TwoDimArray? _spellsTable;
    private static TwoDimArray SpellsTable => _spellsTable ??= NwGameTables.GetTable("spells")!;

    public override SpellLearningView View { get; }
    private NuiWindowToken _token;
    private NuiWindowToken? _descriptionToken;
    private NuiWindow? _window;
    private Action<ModuleEvents.OnNuiEvent>? _descriptionEventHandler;

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

    // Spell swap tracking
    private readonly HashSet<int> _spellsMarkedForRemoval = new(); // Known spells player wants to swap out
    private readonly Dictionary<int, int> _totalSpellsAllowedPerLevel = new(); // Total spells allowed at each level

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
        else if (eventData.ElementId == "spell_desc_button")
        {
            // Show spell description popup
            int rowIndex = eventData.ArrayIndex;
            if (rowIndex >= 0 && rowIndex < _visibleSpellListRows.Count)
            {
                SpellListRow row = _visibleSpellListRows[rowIndex];
                ShowSpellDescription(row);
            }
        }
    }

    private void HandleSpellToggle(SpellListRow row)
    {
        int spellId = row.SpellId;
        int spellLevel = row.SpellLevel;
        int totalAllowed = _totalSpellsAllowedPerLevel.GetValueOrDefault(spellLevel, 0);

        // Calculate current count: known spells (minus those marked for removal) + selected new spells
        int knownCount = _allSpells.TryGetValue(spellLevel, out var spellsAtLevel)
            ? spellsAtLevel.Count(s => s.AlreadyKnown && !_spellsMarkedForRemoval.Contains(s.SpellId))
            : 0;
        int selectedCount = _selectedCountPerLevel.GetValueOrDefault(spellLevel, 0);
        int currentTotal = knownCount + selectedCount;

        if (row.AlreadyKnown)
        {
            // Toggle removal of known spell (for swapping)
            if (_spellsMarkedForRemoval.Contains(spellId))
            {
                // Unmark for removal - but check if we have room
                if (currentTotal >= totalAllowed)
                {
                    _player.SendServerMessage($"You already have the maximum {totalAllowed} spell(s) at level {spellLevel}. Remove a selected spell first.", ColorConstants.Orange);
                    return;
                }

                _spellsMarkedForRemoval.Remove(spellId);
                row.MarkedForRemoval = false;
                Log.Info($"Unmarked known spell {spellId} (Level {spellLevel}) for removal");
            }
            else
            {
                // Mark for removal
                _spellsMarkedForRemoval.Add(spellId);
                row.MarkedForRemoval = true;
                Log.Info($"Marked known spell {spellId} (Level {spellLevel}) for removal (swap)");
            }
        }
        else
        {
            // Toggle selection of new spell
            if (_selectedSpellIds.Contains(spellId))
            {
                _selectedSpellIds.Remove(spellId);
                _selectedCountPerLevel[spellLevel] = selectedCount - 1;
                row.IsSelected = false;
                Log.Info($"Deselected spell {spellId} (Level {spellLevel})");
            }
            else
            {
                // Check if we can add more
                if (currentTotal >= totalAllowed)
                {
                    _player.SendServerMessage($"You can only have {totalAllowed} spell(s) at level {spellLevel}. Remove a known spell or deselect one first.", ColorConstants.Orange);
                    return;
                }

                _selectedSpellIds.Add(spellId);
                _selectedCountPerLevel[spellLevel] = selectedCount + 1;
                row.IsSelected = true;
                Log.Info($"Selected spell {spellId} (Level {spellLevel})");
            }
        }

        // Update UI
        UpdateSpellListBindings();
    }

    private void ConfirmSelections()
    {
        if (_player.ControlledCreature == null)
            return;

        NwCreature creature = _player.ControlledCreature;

        // VALIDATION: Check if the player still qualifies for these spells
        // This handles the case where a prestige class level was removed (e.g., Dragon Disciple
        // removed due to invalid racial template like Aasimar)
        int currentEffectiveCL = EffectiveCasterLevelCalculator.GetEffectiveCasterLevelForClass(creature, _baseClass);

        if (currentEffectiveCL < _effectiveCasterLevel)
        {
            Log.Warn($"{creature.Name}: Effective CL dropped from {_effectiveCasterLevel} to {currentEffectiveCL} - prestige class level was likely removed. Denying spell selection.");
            _player.SendServerMessage(
                "Your prestige class level was removed before you finished selection. You are no longer eligible for these spells.",
                ColorConstants.Red);
            Close();
            return;
        }

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

        // First, remove any spells marked for removal (swap out)
        int spellsRemoved = 0;
        foreach (int spellId in _spellsMarkedForRemoval)
        {
            SpellListRow? row = _allSpells.Values
                .SelectMany(list => list)
                .FirstOrDefault(r => r.SpellId == spellId);

            if (row == null)
                continue;

            Log.Info($"Removing spell {spellId} (Level {row.SpellLevel}) from {creature.Name}'s {_baseClass} spellbook (swap)");
            CreaturePlugin.RemoveKnownSpell(creature, classId, row.SpellLevel, spellId);

            // Also remove any persistent tracking if it was a prestige spell
            string persistentKey = $"PRESTIGE_SPELL_{_baseClass}_{row.SpellLevel}_{spellId}";
            pcKey.GetObjectVariable<LocalVariableInt>(persistentKey).Delete();

            spellsRemoved++;
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

        string successMsg = _selectedSpellIds.Count > 0
            ? $"Successfully learned {_selectedSpellIds.Count} new spell(s)!"
            : "";

        if (spellsRemoved > 0)
        {
            successMsg = string.IsNullOrEmpty(successMsg)
                ? $"Successfully removed {spellsRemoved} spell(s)."
                : $"{successMsg} Removed {spellsRemoved} spell(s).";
        }

        if (!string.IsNullOrEmpty(successMsg))
        {
            _player.SendServerMessage(successMsg, ColorConstants.Green);
        }

        Log.Info($"{creature.Name} learned {_selectedSpellIds.Count} spells, removed {spellsRemoved} spells for {_baseClass} at character level {currentLevel}");

        Close();
    }

    private void ShowSpellDescription(SpellListRow row)
    {
        // Get spell description from the SpellDesc column in spells.2da
        string? spellDescStrRefStr = SpellsTable.GetString(row.SpellId, "SpellDesc");

        if (string.IsNullOrEmpty(spellDescStrRefStr) || spellDescStrRefStr == "****")
        {
            _player.SendServerMessage("No description available for this spell.", ColorConstants.Orange);
            return;
        }

        if (!int.TryParse(spellDescStrRefStr, out int spellDescStrRef))
        {
            _player.SendServerMessage("Could not load spell description.", ColorConstants.Orange);
            return;
        }

        // Get the actual description text from the TLK file
        string description = NWScript.GetStringByStrRef(spellDescStrRef);

        if (string.IsNullOrEmpty(description))
        {
            _player.SendServerMessage("No description available for this spell.", ColorConstants.Orange);
            return;
        }

        // Create a simple popup window with the spell description
        NuiColumn layout = new()
        {
            Children =
            {
                new NuiSpacer { Height = 10f },
                new NuiRow
                {
                    Height = 44f,
                    Children =
                    {
                        new NuiSpacer(),
                        new NuiImage(row.SpellIconResRef)
                        {
                            Height = 40f,
                            Width = 40f,
                            ImageAspect = NuiAspect.Exact
                        },
                        new NuiSpacer()
                    }
                },
                new NuiSpacer { Height = 10f },
                new NuiRow
                {
                    Height = 280f,
                    Children =
                    {
                        new NuiText(description)
                        {
                            Border = false
                        }
                    }
                },
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    {
                        new NuiSpacer(),
                        new NuiButton("Close")
                        {
                            Id = "close_desc",
                            Width = 80f
                        },
                        new NuiSpacer()
                    }
                }
            }
        };

        NuiWindow descWindow = new(layout, row.SpellName)
        {
            Geometry = new NuiRect(300f, 150f, 400f, 460f),
            Closable = true,
            Resizable = false
        };

        // Close any existing description window and unsubscribe its event handler
        CloseDescriptionWindow();

        if (_player.TryCreateNuiWindow(descWindow, out NuiWindowToken newToken))
        {
            _descriptionToken = newToken;

            // Subscribe to module-level NUI events for this window
            _descriptionEventHandler = (eventData) =>
            {
                if (_descriptionToken != null && eventData.Token == _descriptionToken)
                {
                    if (eventData.EventType == NuiEventType.Click && eventData.ElementId == "close_desc")
                    {
                        CloseDescriptionWindow();
                    }
                    else if (eventData.EventType == NuiEventType.Close)
                    {
                        // Clean up when window is closed via X button
                        CleanupDescriptionEventHandler();
                        _descriptionToken = null;
                    }
                }
            };

            NwModule.Instance.OnNuiEvent += _descriptionEventHandler;
        }
    }

    private void CloseDescriptionWindow()
    {
        _descriptionToken?.Close();
        CleanupDescriptionEventHandler();
        _descriptionToken = null;
    }

    private void CleanupDescriptionEventHandler()
    {
        if (_descriptionEventHandler != null)
        {
            NwModule.Instance.OnNuiEvent -= _descriptionEventHandler;
            _descriptionEventHandler = null;
        }
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
        // Close description window if open
        CloseDescriptionWindow();
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
        _totalSpellsAllowedPerLevel.Clear();

        // First, add levels where the player gains new spells
        foreach (var kvp in _spellsNeeded)
        {
            _spellsToLearnPerLevel[kvp.Key] = kvp.Value;
            _selectedCountPerLevel[kvp.Key] = 0;
        }

        // Now calculate total allowed for ALL levels where the player has known spells
        // This allows swapping at any level, not just levels where they gain new spells
        NwClass? nwClass = NwClass.FromClassType(_baseClass);
        if (nwClass == null) return;

        for (int spellLevel = 0; spellLevel <= 9; spellLevel++)
        {
            // Count how many spells they already know at this level
            int knownAtLevel = _knownSpellIds.Count(spellId =>
            {
                NwSpell? spell = NwSpell.FromSpellId(spellId);
                if (spell == null) return false;
#pragma warning disable CS0618
                byte level = spell.GetSpellLevelForClass(nwClass);
#pragma warning restore CS0618
                return level == spellLevel;
            });

            // Skip levels where they have no known spells and no new spells to learn
            if (knownAtLevel == 0 && !_spellsToLearnPerLevel.ContainsKey(spellLevel))
                continue;

            // Get new spells to learn at this level (0 if not in _spellsNeeded)
            int newSpellsToLearn = _spellsToLearnPerLevel.GetValueOrDefault(spellLevel, 0);

            // Ensure this level is in our tracking dictionaries
            if (!_spellsToLearnPerLevel.ContainsKey(spellLevel))
            {
                _spellsToLearnPerLevel[spellLevel] = 0;
                _selectedCountPerLevel[spellLevel] = 0;
            }

            // Total allowed = already known + new they can learn
            _totalSpellsAllowedPerLevel[spellLevel] = knownAtLevel + newSpellsToLearn;

            Log.Info($"Spell level {spellLevel}: can learn {newSpellsToLearn} new spell(s) (total allowed: {_totalSpellsAllowedPerLevel[spellLevel]}, currently known: {knownAtLevel})");
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
        _token.SetBindValue(View.InstructionText, "Select spells to learn. Click known spells to swap them out.");

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
            int newSpells = _spellsNeeded.GetValueOrDefault(level, 0);
            int current = _selectedCountPerLevel.GetValueOrDefault(level, 0);

            // Show new spells to learn if any, otherwise just the level number for swap-only levels
            if (newSpells > 0)
            {
                _token.SetBindValue(textBind, $"{level} ({current}/{newSpells})");
            }
            else
            {
                // Swap-only level - just show the level number
                _token.SetBindValue(textBind, $"{level} (--)");
            }
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
        int newSpellSlots = _spellsNeeded.GetValueOrDefault(_currentSpellLevel, 0);
        int selectedSpells = _selectedCountPerLevel.GetValueOrDefault(_currentSpellLevel, 0);

        // Count how many known spells at this level are marked for removal (opens up additional slots)
        int removedAtLevel = _visibleSpellListRows.Count(r => r.AlreadyKnown && r.MarkedForRemoval);

        // Available slots = new spell slots + removed spells - already selected
        int availableSlots = newSpellSlots + removedAtLevel - selectedSpells;

        string headerText;
        if (newSpellSlots > 0)
        {
            headerText = $"Spell Level {_currentSpellLevel}: Select {availableSlots} more spell(s).";
        }
        else
        {
            // Swap-only level
            headerText = $"Spell Level {_currentSpellLevel}: Swap spells. ({removedAtLevel} removed, {selectedSpells} selected)";
        }
        _token.SetBindValue(View.SpellLevelHeaderText, headerText);

        // Update the visible spell list count
        _token.SetBindValue(View.SpellListCount, _visibleSpellListRows.Count);

        // Update spell buttons for visible spells only
        List<string> buttonTexts = new();
        List<string> tooltips = new();
        List<bool> enabledStates = new();
        List<string> statusTexts = new();
        List<bool> descButtonEnabled = new();
        List<Color> buttonColors = new();
        List<string> iconResRefs = new();

        // Color definitions
        Color greenColor = new Color(32, 200, 32);      // Green for adding
        Color orangeColor = new Color(240, 160, 32);    // Orange for removing
        Color lightBlueColor = new Color(128, 192, 255); // Light blue for known
        Color defaultColor = new Color(255, 255, 255);  // White/default

        foreach (SpellListRow row in _visibleSpellListRows)
        {
            string buttonText;
            Color buttonColor;

            if (row.AlreadyKnown)
            {
                if (row.MarkedForRemoval)
                {
                    // Orange for spells being removed
                    buttonText = $"  - {row.SpellName}";
                    buttonColor = orangeColor;
                }
                else
                {
                    // Light blue for known spells
                    buttonText = $"    {row.SpellName}";
                    buttonColor = lightBlueColor;
                }
            }
            else if (row.IsSelected)
            {
                // Green for spells being added
                buttonText = $"  + {row.SpellName}";
                buttonColor = greenColor;
            }
            else
            {
                // Default - no prefix, white color
                buttonText = $"    {row.SpellName}";
                buttonColor = defaultColor;
            }

            buttonTexts.Add(buttonText);
            buttonColors.Add(buttonColor);

            // Update tooltips based on state
            string tooltip;
            if (row.AlreadyKnown)
            {
                tooltip = row.MarkedForRemoval
                    ? $"Click to keep {row.SpellName}"
                    : $"Click to remove {row.SpellName} (--)";
            }
            else
            {
                tooltip = row.IsSelected
                    ? $"Click to deselect {row.SpellName}"
                    : $"Learn {row.SpellName}";
            }
            tooltips.Add(tooltip);

            // All spells are now enabled (known spells can be toggled for swap)
            enabledStates.Add(true);

            // Status text shows the state
            string statusText;
            if (row.AlreadyKnown)
            {
                statusText = row.MarkedForRemoval ? "(Remove)" : "(Known)";
            }
            else
            {
                statusText = row.IsSelected ? "(Selected)" : "";
            }
            statusTexts.Add(statusText);

            // Description button is always enabled
            descButtonEnabled.Add(true);

            // Add spell icon resref for DrawList overlay
            iconResRefs.Add(row.SpellIconResRef);
        }

        _token.SetBindValues(View.SpellButtonText, buttonTexts);
        _token.SetBindValues(View.SpellTooltip, tooltips);
        _token.SetBindValues(View.SpellEnabled, enabledStates);
        _token.SetBindValues(View.SpellStatusText, statusTexts);
        _token.SetBindValues(View.SpellDescButtonEnabled, descButtonEnabled);
        _token.SetBindValues(View.SpellButtonColor, buttonColors);
        _token.SetBindValues(View.SpellIconResRef, iconResRefs);

        // Check if all required NEW spells are selected (only levels from _spellsNeeded, not swap-only levels)
        bool allSelected = _spellsNeeded.All(kvp =>
            _selectedCountPerLevel.GetValueOrDefault(kvp.Key, 0) >= kvp.Value);

        bool hasChanges = _selectedSpellIds.Count > 0 || _spellsMarkedForRemoval.Count > 0;

        // Can confirm if all new slots are filled, OR if they've made any changes (swaps count)
        _token.SetBindValue(View.CanConfirm, allSelected || hasChanges);
    }

    private void UpdateSpellLevelButton(int level, NuiBind<string> textBind)
    {
        if (_spellsToLearnPerLevel.ContainsKey(level))
        {
            int newSpells = _spellsNeeded.GetValueOrDefault(level, 0);
            int current = _selectedCountPerLevel.GetValueOrDefault(level, 0);

            // Show new spells to learn if any, otherwise just the level number for swap-only levels
            if (newSpells > 0)
            {
                _token.SetBindValue(textBind, $"{level} ({current}/{newSpells})");
            }
            else
            {
                // Swap-only level - just show the level number
                _token.SetBindValue(textBind, $"{level} (--)");
            }
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
        public int SpellDescStrRef { get; set; }
        public bool AlreadyKnown { get; set; }
        public bool IsSelected { get; set; }
        public bool MarkedForRemoval { get; set; } // For spell swap feature
    }
}

