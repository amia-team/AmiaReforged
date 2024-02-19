using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Castle.Components.DictionaryAdapter;
using NLog;
using NLog.Fluent;

namespace AmiaReforged.System.Services;

public class SpellbookService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private NuiWindowToken _token;
    private NuiWindow _window;
    private List<string> ButtonIds { get; set; } = new();

    public SpellbookService()
    {
        NwModule.Instance.OnNuiEvent += OnSpellbookButtonClick;
    }

    private void OnSpellbookButtonClick(ModuleEvents.OnNuiEvent obj)
    {
        bool clickedClassSpellbook = ButtonIds.Contains(obj.ElementId) && obj.EventType != NuiEventType.MouseUp;
        if (!clickedClassSpellbook) return;

        string className = obj.ElementId.Replace("SpellbookButton", "");

        CreatureClassInfo? classInfo =
            obj.Player.LoginCreature?.Classes.Where(c => c.Class.Name.ToString() == className).First();


        Dictionary<byte, IReadOnlyList<MemorizedSpellSlot>> preparedSpells = PreparedSpells(classInfo);


        List<NuiImage> spellLevelIcons = new()
        {
            new NuiImage("ir_level789"),
            new NuiImage("ir_level1"),
            new NuiImage("ir_level2"),
            new NuiImage("ir_level3"),
            new NuiImage("ir_level4"),
            new NuiImage("ir_level5"),
            new NuiImage("ir_level6"),
            new NuiImage("ir_level789"),
            new NuiImage("ir_level789"),
            new NuiImage("ir_level789")
        };

        List<NuiRow> spellRows = SpellRows(preparedSpells, spellLevelIcons);


        NuiRow spellBookName = new NuiRow()
        {
            Children = new List<NuiElement>()
            {
                new NuiLabel(className)
            }
        };

        NuiCombo spellBookDropdown = new NuiCombo()
        {
        };

        NuiColumn column = new NuiColumn()
        {
            Children = new EditableList<NuiElement>()
            {
                spellBookName,
                spellBookDropdown,
                new NuiSpacer(),
                spellRows[0],
                spellRows[1],
                spellRows[2],
                spellRows[3],
                spellRows[4],
                spellRows[5],
                spellRows[6],
                spellRows[7],
                spellRows[8],
                spellRows[9],
                new NuiRow()
                {
                    Children = new List<NuiElement>()
                    {
                        new NuiButton("Save Spell Book")
                        {
                            Enabled = false
                        },
                        new NuiButton("Delete Spell Book")
                        {
                            Enabled = false
                        }
                    }
                }
            }
        };

        NuiWindow window = new(column, "Spellbooks")
        {
            Closable = true,
            Geometry = new NuiRect(500f, 100f, 300f, 400f)
        };
        _token.Close();

        obj.Player.TryCreateNuiWindow(window, out NuiWindowToken token);

        _token = token;
    }

    private static List<NuiRow> SpellRows(Dictionary<byte, IReadOnlyList<MemorizedSpellSlot>> preparedSpells, List<NuiImage> spellLevelIcons)
    {
        Dictionary<int, List<NuiImage>> spellRow = new Dictionary<int, List<NuiImage>>();
        List<NuiRow> spellRows = new List<NuiRow>();
        for (byte f = 0; f <= 9; f++)
        {
            Log.Info($"Iteration {f}");
            spellRows.Add(new NuiRow());
            if (!preparedSpells.ContainsKey(f)) break;
            Log.Info($"Spell level {f} has {preparedSpells[f].Count} spells.");
            spellRow.TryAdd(f, new List<NuiImage>());
            spellRow[f].Add(spellLevelIcons[f]);

            foreach (MemorizedSpellSlot s in preparedSpells[f])
            {
                if (!s.IsPopulated) continue;

                string? spellIconResRef = s.Spell.IconResRef;
                if (spellIconResRef == null) continue;

                NuiImage prep = new NuiImage(spellIconResRef)
                {
                    Tooltip = s.Spell.Name.ToString()
                };

                spellRow[f].Add(prep);
            }

            spellRows[f] = new NuiRow
            {
                Children = new List<NuiElement>(spellRow[f])
            };
        }

        return spellRows;
    }

    private static Dictionary<byte, IReadOnlyList<MemorizedSpellSlot>> PreparedSpells(CreatureClassInfo? classInfo)
    {
        Dictionary<byte, IReadOnlyList<MemorizedSpellSlot>> preparedSpells = new();

        for (byte i = 0; i <= 9; i++)
        {
            if (classInfo is null || classInfo.GetMemorizedSpellSlots(i).Count == 0) continue;

            preparedSpells.TryAdd(i, classInfo.GetMemorizedSpellSlots(i));
        }

        return preparedSpells;
    }

    public void OpenSpellbookWindow(ModuleEvents.OnNuiEvent obj)
    {
        List<string> classNames = new();

        foreach (CreatureClassInfo charClassInfo in obj.Player.LoginCreature?.Classes!)
        {
            if (charClassInfo.Class.IsSpellbookRestricted)
                classNames.Add(charClassInfo.Class.Name.ToString());

            if (charClassInfo.Class.HasDomains)
                classNames.Add(charClassInfo.Class.Name.ToString());
        }

        bool hasNoCasterClasses = classNames.Count is 0;
        if (hasNoCasterClasses)
        {
            obj.Player.SendServerMessage("You have no spellbooks to view.", Color.FromRGBA("#FF0000"));
            return;
        }

        List<NuiButton> classButtons = new List<NuiButton>();
        NuiRow classes = new NuiRow()
        {
            Children = new List<NuiElement>(classButtons),
        };

        foreach (string className in classNames)
        {
            string id = $"{className}SpellbookButton";

            NuiButton classButton = new NuiButton(className)
            {
                Tooltip = $"Click here to see your {className} spellbooks.",
                Enabled = true,
                Id = id
            };

            classButtons.Add(classButton);
            ButtonIds?.Add(id);
        }


        NuiColumn root = new NuiColumn()
        {
            Children = new List<NuiElement>(classButtons)
        };

        NuiWindow window = new(root, "Spellbooks")
        {
            Closable = true,
            Geometry = new NuiRect(500f, 100f, 300f, 400f)
        };

        window.Id = "spellbookWindow";

        obj.Player.TryCreateNuiWindow(window, out NuiWindowToken token);

        _token = token;
    }
}