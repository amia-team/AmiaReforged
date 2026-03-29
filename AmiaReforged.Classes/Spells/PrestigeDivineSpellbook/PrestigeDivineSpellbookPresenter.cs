using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Spells.PrestigeDivineSpellbook;

/// <summary>
/// Presenter for the Prestige Divine Spellbook window. Handles spell selection and memorization
/// for Rangers/Paladins with prestige class caster level boosts.
/// </summary>
public sealed class PrestigeDivineSpellbookPresenter : ScryPresenter<PrestigeDivineSpellbookView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    // Cache the spells.2da table for spell descriptions
    private static TwoDimArray? _spellsTable;
    private static TwoDimArray SpellsTable => _spellsTable ??= NwGameTables.GetTable("spells")!;

    public override PrestigeDivineSpellbookView View { get; }

    private readonly NwPlayer _player;
    private readonly NwCreature _creature;
    private readonly ClassType _classType;
    private readonly CreatureClassInfo _classInfo;
    private readonly DivineSpellCache _spellCache;

    // UI state
    private int _currentSpellLevel = 1;
    private readonly Dictionary<int, List<(int spellId, string name)>> _availableSpells = new();
    private readonly Dictionary<int, HashSet<int>> _memorizedSpellIds = new();
    private readonly List<(int spellId, string name)> _currentLevelSpells = new();

    public PrestigeDivineSpellbookPresenter(PrestigeDivineSpellbookView view, NwPlayer player, ClassType classType, NwCreature creature)
    {
        View = view;
        _player = player;
        _creature = creature;
        _classType = classType;
        _classInfo = creature.GetClassInfo(classType)!;
        _spellCache = new DivineSpellCache();
    }

    private NuiWindowToken? _token;

    // ...existing code...

    public override NuiWindowToken Token() => _token ?? NuiWindowToken.Invalid;

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType == NuiEventType.Click)
        {
            HandleButtonClick(eventData);
        }
    }

    public override void InitBefore()
    {
        // Called before window creation - initialize all data structures
        int effectiveLevel = EffectiveCasterLevelCalculator.GetEffectiveCasterLevelForClass(_creature, _classType);

        // Load all available spells for this class
        LoadAvailableSpells(effectiveLevel);

        // Load currently memorized spells
        LoadMemorizedSpells();

        // Set first available spell level
        int maxCircle = DivineSpellProgressionData.GetMaxSpellCircleForCasterLevel(_classType, effectiveLevel);
        for (int level = 1; level <= 4; level++)
        {
            if (level <= maxCircle)
            {
                _currentSpellLevel = level;
                break;
            }
        }
    }

    public override void Create()
    {
        // Create the NUI window
        NuiWindow window = new NuiWindow(View.RootLayout(), $"Prestige Divine Spellbook - {_classType}")
        {
            Geometry = new NuiRect(100f, 100f, 900f, 600f),
            Resizable = true,
            Closable = true
        };

        // Create the window and get the token
        if (_player.TryCreateNuiWindow(window, out NuiWindowToken token))
        {
            _token = token;
            UpdateView();
        }
    }

    public override void UpdateView()
    {
        if (_token == null || _token.Value == NuiWindowToken.Invalid)
        {
            return;
        }

        int effectiveLevel = EffectiveCasterLevelCalculator.GetEffectiveCasterLevelForClass(_creature, _classType);
        int maxCircle = DivineSpellProgressionData.GetMaxSpellCircleForCasterLevel(_classType, effectiveLevel);

        // Set header and instruction text
        var token = _token.Value;
        token.SetBindValue(View.HeaderText, $"Prestige Divine Spellbook");
        token.SetBindValue(View.InstructionText, "Select spells to memorize to your available spell slots");
        token.SetBindValue(View.ClassNameText, $"Class: {_classType}");
        token.SetBindValue(View.CasterLevelText, $"Caster Level: {effectiveLevel} (Base: {_classInfo.Level})");

        // Set spell level button states
        for (int level = 1; level <= 4; level++)
        {
            bool isAvailable = level <= maxCircle;
            bool isSelected = level == _currentSpellLevel;

            var buttonTextBind = level switch
            {
                1 => View.SpellLevelButtonText1,
                2 => View.SpellLevelButtonText2,
                3 => View.SpellLevelButtonText3,
                4 => View.SpellLevelButtonText4,
                _ => null
            };

            var enableBind = level switch
            {
                1 => View.SpellLevelButtonEnabled1,
                2 => View.SpellLevelButtonEnabled2,
                3 => View.SpellLevelButtonEnabled3,
                4 => View.SpellLevelButtonEnabled4,
                _ => null
            };

            if (buttonTextBind != null && enableBind != null)
            {
                string buttonText = $"Level {level}{(isSelected ? " ✓" : "")}";
                token.SetBindValue(buttonTextBind, buttonText);
                token.SetBindValue(enableBind, isAvailable);
            }
        }

        // Update spell list
        RefreshSpellList();
        UpdateSpellListDisplay(token);

        // Update slots info
        UpdateSlotsDisplay(token);
    }

    private void UpdateSpellListDisplay(NuiWindowToken token)
    {
        // Set visibility and values for each spell in the list
        for (int i = 0; i < View.SpellNames.Count; i++)
        {
            if (i < _currentLevelSpells.Count)
            {
                var (spellId, name) = _currentLevelSpells[i];
                bool isMemoized = _memorizedSpellIds.TryGetValue(_currentSpellLevel, out var mems) && mems.Contains(spellId);

                token.SetBindValue(View.SpellNames[i], name);
                token.SetBindValue(View.SpellStatus[i], isMemoized ? "✓" : "");
                token.SetBindValue(View.SpellButtonColor[i], isMemoized ? "00FFFF" : "FFFFFF");
                token.SetBindValue(View.SpellVisible[i], true);
            }
            else
            {
                // Hide unused rows
                token.SetBindValue(View.SpellVisible[i], false);
            }
        }
    }

    private void UpdateSlotsDisplay(NuiWindowToken token)
    {
        int maxSlots = CreaturePlugin.GetMaxSpellSlots(_creature, _classInfo.Class.Id, _currentSpellLevel);
        int memorized = _memorizedSpellIds.TryGetValue(_currentSpellLevel, out var mems) ? mems.Count : 0;
        token.SetBindValue(View.SlotsInfoText, $"Spells Memorized: {memorized} / {maxSlots}");
    }

    private void LoadAvailableSpells(int effectiveLevel)
    {
        _availableSpells.Clear();

        int maxCircle = DivineSpellProgressionData.GetMaxSpellCircleForCasterLevel(_classType, effectiveLevel);

        for (int level = 1; level <= maxCircle; level++)
        {
            var spells = _spellCache.GetSpellsForClass(_classType, level);
            _availableSpells[level] = spells.Select(id => (id, GetSpellName(id))).ToList();
        }
    }

    private void LoadMemorizedSpells()
    {
        _memorizedSpellIds.Clear();

        for (int level = 1; level <= 4; level++)
        {
            _memorizedSpellIds[level] = new HashSet<int>();

            var knownSpells = _classInfo.KnownSpells.ElementAtOrDefault(level);
            if (knownSpells != null)
            {
                foreach (var spell in knownSpells)
                {
                    _memorizedSpellIds[level].Add(spell.Id);
                }
            }
        }
    }

    private void RefreshSpellList()
    {
        _currentLevelSpells.Clear();

        if (_availableSpells.TryGetValue(_currentSpellLevel, out var spells))
        {
            _currentLevelSpells.AddRange(spells);
        }

        Log.Debug($"Refreshed spell list for level {_currentSpellLevel}: {_currentLevelSpells.Count} spells");
    }

    private void UpdateSlotsInfo()
    {
        int maxSlots = CreaturePlugin.GetMaxSpellSlots(_creature, _classInfo.Class.Id, _currentSpellLevel);
        int memorized = _memorizedSpellIds.TryGetValue(_currentSpellLevel, out var mems) ? mems.Count : 0;
        Log.Debug($"Slots info for level {_currentSpellLevel}: {memorized} / {maxSlots}");
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.ElementId.StartsWith("spell_level_button_"))
        {
            string levelStr = eventData.ElementId.Replace("spell_level_button_", "");
            if (int.TryParse(levelStr, out int level))
            {
                _currentSpellLevel = level;
                UpdateView();
            }
        }
        else if (eventData.ElementId.StartsWith("spell_button_"))
        {
            string indexStr = eventData.ElementId.Replace("spell_button_", "");
            if (int.TryParse(indexStr, out int rowIndex) && rowIndex >= 0 && rowIndex < _currentLevelSpells.Count)
            {
                HandleSpellClick(_currentLevelSpells[rowIndex]);
                UpdateView();
            }
        }
        else if (eventData.ElementId == "confirm_button")
        {
            Close();
        }
    }

    private void HandleSpellClick((int spellId, string name) spell)
    {
        var memorized = _memorizedSpellIds[_currentSpellLevel];
        bool alreadyMemorized = memorized.Contains(spell.spellId);

        if (alreadyMemorized)
        {
            try
            {
                CreaturePlugin.RemoveKnownSpell(_creature, _classInfo.Class.Id, _currentSpellLevel, spell.spellId);
                memorized.Remove(spell.spellId);
            }
            catch (Exception ex)
            {
                Log.Error($"Error removing spell: {ex.Message}");
            }
        }
        else
        {
            try
            {
                CreaturePlugin.AddKnownSpell(_creature, _classInfo.Class.Id, _currentSpellLevel, spell.spellId);
                memorized.Add(spell.spellId);
            }
            catch (Exception ex)
            {
                Log.Error($"Error adding spell: {ex.Message}");
            }
        }
    }

    private string GetSpellName(int spellId)
    {
        try
        {
            return SpellsTable.GetString(spellId, 0) ?? $"Spell {spellId}";
        }
        catch
        {
            return $"Spell {spellId}";
        }
    }

    public override void Close()
    {
        // Window closed
    }
}










