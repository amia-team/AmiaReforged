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
        Log.Info($"Initializing Prestige Divine Spellbook for {_creature.Name} - {_classType}");

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
        Log.Info($"Creating Prestige Divine Spellbook window for {_creature.Name} - {_classType}");

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
            Log.Info($"✓ NUI window created successfully for {_creature.Name}");
        }
        else
        {
            Log.Error($"Failed to create NUI window for {_creature.Name}");
            _player.SendServerMessage("Error creating spellbook window", ColorConstants.Red);
        }
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

        Log.Info($"Loaded {_availableSpells.Sum(x => x.Value.Count)} available spells for {_classType}");
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

        Log.Info($"Loaded memorized spells for {_classType}");
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
        Log.Debug($"Button clicked: {eventData.ElementId}");

        if (eventData.ElementId.StartsWith("spell_level_button_"))
        {
            string levelStr = eventData.ElementId.Replace("spell_level_button_", "");
            if (int.TryParse(levelStr, out int level))
            {
                _currentSpellLevel = level;
                RefreshSpellList();
                UpdateSlotsInfo();
            }
        }
        else if (eventData.ElementId == "spell_button")
        {
            int rowIndex = eventData.ArrayIndex;
            if (rowIndex >= 0 && rowIndex < _currentLevelSpells.Count)
            {
                HandleSpellClick(_currentLevelSpells[rowIndex]);
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
            // Remove the spell
            try
            {
                CreaturePlugin.RemoveKnownSpell(_creature, _classInfo.Class.Id, _currentSpellLevel, spell.spellId);
                memorized.Remove(spell.spellId);
                Log.Info($"Removed spell {spell.name} from {_creature.Name}'s {_classType} memorization");
            }
            catch (Exception ex)
            {
                Log.Error($"Error removing spell: {ex.Message}");
                _player.SendServerMessage($"Error removing spell: {ex.Message}", ColorConstants.Red);
            }
        }
        else
        {
            // Add the spell
            try
            {
                CreaturePlugin.AddKnownSpell(_creature, _classInfo.Class.Id, _currentSpellLevel, spell.spellId);
                memorized.Add(spell.spellId);
                Log.Info($"Added spell {spell.name} to {_creature.Name}'s {_classType} memorization");
            }
            catch (Exception ex)
            {
                Log.Error($"Error adding spell: {ex.Message}");
                _player.SendServerMessage($"Error adding spell: {ex.Message}", ColorConstants.Red);
            }
        }

        UpdateSlotsInfo();
        RefreshSpellList();
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
        Log.Info($"Closing Prestige Divine Spellbook for {_creature.Name}");
        _player.SendServerMessage("Spellbook closed. Your selections have been saved.", ColorConstants.Cyan);
    }
}










