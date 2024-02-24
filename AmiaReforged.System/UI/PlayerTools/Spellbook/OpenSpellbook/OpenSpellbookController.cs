using AmiaReforged.Core.Services;
using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.Spellbook.OpenSpellbook;

public class OpenSpellbookController : WindowController<OpenSpellbookView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    [Inject] private Lazy<SpellbookLoaderService> SpellbookLoader { get; set; }


    private SpellbookViewModel _spellbook;

    public override void Init()
    {
        string spellbookIdString = NWScript.GetLocalString(Token.Player.LoginCreature, "selected_spellbook");

        Guid spellbookId = Guid.Parse(spellbookIdString);

        _spellbook = SpellbookLoader.Value.LoadSingleSpellbook(spellbookId);

        Log.Info(_spellbook.ToString());

        PopulateSpells();
    }

    private void PopulateSpells()
    {
        string className = Token.Player.LoginCreature!.Classes.Where(c => c.Class.Id == int.Parse(_spellbook.Class))
            .Select(c => c.Class.Name).FirstOrDefault().ToString();

        Token.SetBindValue(View.SpellbookName, $"Spellbook: {_spellbook.Name}");
        Token.SetBindValue(View.SpellbookClass, $"For Class: {className}");


        NuiColumn spells = ProcessSpellsToNuiColumn();

        Token.SetGroupLayout(View.Spells, spells);
    }

    private NuiColumn ProcessSpellsToNuiColumn()
    {
        List<NuiImage> spellLevelIcons = SpellbookLayout();

        List<NuiRow> spellRows = PopulateSpellRows(_spellbook.SpellBook, spellLevelIcons);

        NuiColumn spells = new NuiColumn()
        {
            Height = 500f
        };

        spells.Children.Add(spellRows[0]);
        spells.Children.Add(spellRows[1]);
        spells.Children.Add(spellRows[2]);
        spells.Children.Add(spellRows[3]);
        spells.Children.Add(spellRows[4]);
        spells.Children.Add(spellRows[5]);
        spells.Children.Add(spellRows[6]);
        spells.Children.Add(spellRows[7]);
        spells.Children.Add(spellRows[8]);
        spells.Children.Add(spellRows[9]);
        return spells;
    }

    private static List<NuiImage> SpellbookLayout()
    {
        return new()
        {
            new NuiImage("ir_level789")
            {
                Tooltip = "Cantrips",
                Height = 40f,
                Width = 40f,
                ImageAspect = NuiAspect.Fill,
                HorizontalAlign = NuiHAlign.Center,
                VerticalAlign = NuiVAlign.Middle
            },
            new NuiImage("ir_level1")
            {
                Tooltip = "Level 1",
                Height = 40f,
                Width = 40f,
                ImageAspect = NuiAspect.Fill,
                HorizontalAlign = NuiHAlign.Center,
                VerticalAlign = NuiVAlign.Middle
            },
            new NuiImage("ir_level2")
            {
                Tooltip = "Level 2",
                Height = 40f,
                Width = 40f,
                ImageAspect = NuiAspect.Fill,
                HorizontalAlign = NuiHAlign.Center,
                VerticalAlign = NuiVAlign.Middle
            },
            new NuiImage("ir_level3")
            {
                Tooltip = "Level 3",
                Height = 40f,
                Width = 40f,
                ImageAspect = NuiAspect.Fill,
                HorizontalAlign = NuiHAlign.Center,
                VerticalAlign = NuiVAlign.Middle
            },
            new NuiImage("ir_level4")
            {
                Tooltip = "Level 4",
                Height = 40f,
                Width = 40f,
                ImageAspect = NuiAspect.Fill,
                HorizontalAlign = NuiHAlign.Center,
                VerticalAlign = NuiVAlign.Middle
            },
            new NuiImage("ir_level5")
            {
                Tooltip = "Level 5",
                Height = 40f,
                Width = 40f,
                ImageAspect = NuiAspect.Fill,
                HorizontalAlign = NuiHAlign.Center,
                VerticalAlign = NuiVAlign.Middle
            },
            new NuiImage("ir_level6")
            {
                Tooltip = "Level 6",
                Height = 40f,
                Width = 40f,
                ImageAspect = NuiAspect.Fill,
                HorizontalAlign = NuiHAlign.Center,
                VerticalAlign = NuiVAlign.Middle
            },
            new NuiImage("ir_level789")
            {
                Tooltip = "Level 7",
                Height = 40f,
                Width = 40f,
                ImageAspect = NuiAspect.Fill,
                HorizontalAlign = NuiHAlign.Center,
                VerticalAlign = NuiVAlign.Middle
            },
            new NuiImage("ir_level789")
            {
                Tooltip = "Level 8",
                Height = 40f,
                Width = 40f,
                ImageAspect = NuiAspect.Fill,
                HorizontalAlign = NuiHAlign.Center,
                VerticalAlign = NuiVAlign.Middle
            },
            new NuiImage("ir_level789")
            {
                Tooltip = "Level 9",
                Height = 40f,
                Width = 40f,
                ImageAspect = NuiAspect.Fill,
                HorizontalAlign = NuiHAlign.Center,
                VerticalAlign = NuiVAlign.Middle
            }
        };
    }

    private static List<NuiRow> PopulateSpellRows(Dictionary<byte, List<PreparedSpellModel>>? preparedSpells,
        List<NuiImage> spellLevelIcons)
    {
        if (preparedSpells == null) return new List<NuiRow>();
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

            foreach (PreparedSpellModel s in preparedSpells[f])
            {
                string spellIconResRef = s.IconResRef;
                if (spellIconResRef == string.Empty)
                {
                    Log.Info($"Spell {s.SpellName} has no icon.");
                    continue;
                }

                Log.Info($"Processing spell {s.SpellName} with icon {spellIconResRef}");

                NuiImage prep = new(spellIconResRef)
                {
                    Tooltip = s.SpellName,
                    Height = 40f,
                    Width = 40f,
                    ImageAspect = NuiAspect.Fill,
                    HorizontalAlign = NuiHAlign.Center,
                    VerticalAlign = NuiVAlign.Middle
                };

                spellRow[f].Add(prep);
            }

            spellRows[f] = new NuiRow
            {
                Height = 50f,
                Visible = true,
                Children = new List<NuiElement>(spellRow[f])
            };
        }

        return spellRows;
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                if (eventData.ElementId == View.CloseSpellbookButton.Id)
                {
                    Token.Close();
                }
                else if (eventData.ElementId == View.LoadSpellbookButton.Id)
                {
                    SpellbookMemorizer memorizer = new(_spellbook, Token.Player);
                    memorizer.MemorizeSpellsToPlayer();
                    Token.Close();
                }

                break;
        }
    }

    protected override void OnClose()
    {
        _spellbook = null!;
    }
}

public sealed class SpellbookMemorizer
{
    private readonly SpellbookViewModel _spellbook;
    private readonly NwPlayer _player;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public SpellbookMemorizer(SpellbookViewModel spellbook, NwPlayer player)
    {
        _spellbook = spellbook;
        _player = player;
    }


    public void MemorizeSpellsToPlayer()
    {
        int classId = int.Parse(_spellbook.Class);
        if (_player.LoginCreature == null) return;

        NwCreature playerCreature = _player.LoginCreature;

        if (!playerCreature.Classes.Any(c => c.Class.Id == classId)) return;
        CreatureClassInfo classInfo = playerCreature.Classes.First(c => c.Class.Id == classId);


        for (byte i = 0; i <= 9; i++)
        {
            IReadOnlyList<PreparedSpellModel> spells = _spellbook.SpellBook[i];

            foreach (MemorizedSpellSlot spellSlot in classInfo.GetMemorizedSpellSlots(i))
            {
                spellSlot.ClearMemorizedSpell();
            }
            Log.Info("Cleared spell slots.");

        }

        for (byte spellLevel = 0; spellLevel <= 9; spellLevel++)
        {
            Log.Info($"Memorizing spells for level {(int)spellLevel}");
            IReadOnlyList<PreparedSpellModel> spells = _spellbook.SpellBook[spellLevel];
            
            for (int spellSlot = 0; spellSlot < classInfo.GetMemorizedSpellSlots(spellLevel).Count; spellSlot++)
            {
                if (spellSlot > spells.Count)
                {
                    Log.Info("No more spells to memorize.");
                    break;
                }
                PreparedSpellModel spell = spells[spellSlot];
                MemorizedSpellSlot memorized = classInfo.GetMemorizedSpellSlots(spellLevel)[spellSlot];
                if (!spell.IsPopulated) continue;

                NwSpell? currentSpell = NwSpell.FromSpellId(spell.SpellId);

                if (currentSpell == null)
                {
                    Log.Info("Spell is not a valid spell.");
                    continue;
                }

                memorized.Spell = currentSpell;
                memorized.MetaMagic = spell.MetaMagic;
                memorized.IsDomainSpell = spell.IsDomainSpell;
                memorized.IsReady = false;
            }
        }
    }
}