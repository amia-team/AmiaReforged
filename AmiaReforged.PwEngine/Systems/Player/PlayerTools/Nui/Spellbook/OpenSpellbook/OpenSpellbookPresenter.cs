using AmiaReforged.Core.Services;
using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Spellbook.OpenSpellbook;

public class OpenSpellbookPresenter(OpenSpellbookView toolView, NwPlayer player) : ScryPresenter<OpenSpellbookView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private SpellbookViewModel _spellbook = null!;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    [Inject] private Lazy<SpellbookLoaderService> SpellbookLoader { get; set; } = null!;

    public override OpenSpellbookView View { get; } = toolView;

    public override NuiWindowToken Token() => _token;


    public override void InitBefore()
    {
        _window = new(View.RootLayout(), View.Title)
        {
            Resizable = false,
            Geometry = new NuiRect(500f, 100f, 580f, 500f)
        };
    }

    public override void Create()
    {
        // Create the window if it's null.
        if (_window == null)
            // Try to create the window if it doesn't exist.
            InitBefore();

        // If the window wasn't created, then tell the user we screwed up.
        if (_window == null)
        {
            player.SendServerMessage(
                message: "The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        player.TryCreateNuiWindow(_window, out _token);

        string spellbookIdString =
            NWScript.GetLocalString(Token().Player.LoginCreature, sVarName: "selected_spellbook");

        long spellbookId = long.Parse(spellbookIdString);

        _spellbook = SpellbookLoader.Value.LoadSingleSpellbook(spellbookId);

        Log.Info(_spellbook.ToString());

        PopulateSpells();
    }

    private void PopulateSpells()
    {
        string className = Token().Player.LoginCreature!.Classes.Where(c => c.Class.Id == int.Parse(_spellbook.Class))
            .Select(c => c.Class.Name).FirstOrDefault().ToString();

        Token().SetBindValue(View.SpellbookName, $"Spellbook: {_spellbook.Name}");
        Token().SetBindValue(View.SpellbookClass, $"For Class: {className}");


        NuiColumn spells = ProcessSpellsToNuiColumn();

        Token().SetGroupLayout(View.Spells, spells);
    }

    private NuiColumn ProcessSpellsToNuiColumn()
    {
        List<NuiImage> spellLevelIcons = SpellbookLayout();

        List<NuiRow> spellRows = PopulateSpellRows(_spellbook.SpellBook, spellLevelIcons);

        NuiColumn spells = new()
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

    private static List<NuiImage> SpellbookLayout() =>
    [
        new(resRef: "ir_level789")
        {
            Tooltip = "Cantrips",
            Height = 40f,
            Width = 40f,
            ImageAspect = NuiAspect.Fill,
            HorizontalAlign = NuiHAlign.Center,
            VerticalAlign = NuiVAlign.Middle
        },

        new(resRef: "ir_level1")
        {
            Tooltip = "Level 1",
            Height = 40f,
            Width = 40f,
            ImageAspect = NuiAspect.Fill,
            HorizontalAlign = NuiHAlign.Center,
            VerticalAlign = NuiVAlign.Middle
        },

        new(resRef: "ir_level2")
        {
            Tooltip = "Level 2",
            Height = 40f,
            Width = 40f,
            ImageAspect = NuiAspect.Fill,
            HorizontalAlign = NuiHAlign.Center,
            VerticalAlign = NuiVAlign.Middle
        },

        new(resRef: "ir_level3")
        {
            Tooltip = "Level 3",
            Height = 40f,
            Width = 40f,
            ImageAspect = NuiAspect.Fill,
            HorizontalAlign = NuiHAlign.Center,
            VerticalAlign = NuiVAlign.Middle
        },

        new(resRef: "ir_level4")
        {
            Tooltip = "Level 4",
            Height = 40f,
            Width = 40f,
            ImageAspect = NuiAspect.Fill,
            HorizontalAlign = NuiHAlign.Center,
            VerticalAlign = NuiVAlign.Middle
        },

        new(resRef: "ir_level5")
        {
            Tooltip = "Level 5",
            Height = 40f,
            Width = 40f,
            ImageAspect = NuiAspect.Fill,
            HorizontalAlign = NuiHAlign.Center,
            VerticalAlign = NuiVAlign.Middle
        },

        new(resRef: "ir_level6")
        {
            Tooltip = "Level 6",
            Height = 40f,
            Width = 40f,
            ImageAspect = NuiAspect.Fill,
            HorizontalAlign = NuiHAlign.Center,
            VerticalAlign = NuiVAlign.Middle
        },

        new(resRef: "ir_level789")
        {
            Tooltip = "Level 7",
            Height = 40f,
            Width = 40f,
            ImageAspect = NuiAspect.Fill,
            HorizontalAlign = NuiHAlign.Center,
            VerticalAlign = NuiVAlign.Middle
        },

        new(resRef: "ir_level789")
        {
            Tooltip = "Level 8",
            Height = 40f,
            Width = 40f,
            ImageAspect = NuiAspect.Fill,
            HorizontalAlign = NuiHAlign.Center,
            VerticalAlign = NuiVAlign.Middle
        },

        new(resRef: "ir_level789")
        {
            Tooltip = "Level 9",
            Height = 40f,
            Width = 40f,
            ImageAspect = NuiAspect.Fill,
            HorizontalAlign = NuiHAlign.Center,
            VerticalAlign = NuiVAlign.Middle
        }
    ];

    private static List<NuiRow> PopulateSpellRows(Dictionary<byte, List<PreparedSpellModel>?>? preparedSpells,
        List<NuiImage> spellLevelIcons)
    {
        if (preparedSpells == null) return [];
        Dictionary<int, List<NuiImage>> spellRow = new();
        List<NuiRow> spellRows = [];
        for (byte f = 0; f <= 9; f++)
        {
            Log.Info($"Iteration {f}");
            spellRows.Add(new());
            if (!preparedSpells.TryGetValue(f, out List<PreparedSpellModel>? _)) break;

            spellRow.TryAdd(f, []);
            spellRow[f].Add(spellLevelIcons[f]);

            foreach (NuiImage prep in from s in preparedSpells[f]
                     let spellIconResRef = s.IconResRef == "" ? "ir_tmp_spawn" : s.IconResRef
                     select new NuiImage(spellIconResRef)
                     {
                         Tooltip = s.SpellName,
                         Height = 40f,
                         Width = 40f,
                         ImageAspect = NuiAspect.Fill,
                         HorizontalAlign = NuiHAlign.Center,
                         VerticalAlign = NuiVAlign.Middle
                     })
            {
                spellRow[f].Add(prep);
            }

            spellRows[f] = new()
            {
                Height = 50f,
                Visible = true,
                Children = new(spellRow[f])
            };
        }

        return spellRows;
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                HandleButtonClick(eventData);
                break;
        }
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.ElementId == View.CloseSpellbookButton.Id)
        {
            Token().Close();
        }
        else if (eventData.ElementId == View.LoadSpellbookButton.Id)
        {
            SpellbookMemorizer memorizer = new(_spellbook, Token().Player);
            memorizer.MemorizeSpellsToPlayer();
            Token().Close();
        }
        else if (eventData.ElementId == View.CloseSpellbookButton.Id)
        {
            Token().Close();
        }
    }

    public override void Close()
    {
        _spellbook = null!;
    }
}

public sealed class SpellbookMemorizer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly NwPlayer _player;
    private readonly SpellbookViewModel _spellbook;

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

        if (playerCreature.Classes.All(c => c.Class.Id != classId)) return;
        CreatureClassInfo classInfo = playerCreature.Classes.First(c => c.Class.Id == classId);


        for (byte i = 0; i <= 9; i++)
        {
            foreach (MemorizedSpellSlot spellSlot in classInfo.GetMemorizedSpellSlots(i))
            {
                spellSlot.ClearMemorizedSpell();
            }

            Log.Info(message: "Cleared spell slots.");
        }

        for (byte spellLevel = 0; spellLevel <= 9; spellLevel++)
        {
            Log.Info($"Memorizing spells for level {(int)spellLevel}");

            IReadOnlyList<PreparedSpellModel>? spells = _spellbook.SpellBook?[spellLevel];

            for (int spellSlot = 0; spellSlot < classInfo.GetMemorizedSpellSlots(spellLevel).Count; spellSlot++)
            {
                if (spells == null) continue;
                if (spellSlot > spells.Count)
                {
                    Log.Info(message: "No more spells to memorize.");
                    break;
                }

                PreparedSpellModel spell = spells[spellSlot];
                MemorizedSpellSlot memorized = classInfo.GetMemorizedSpellSlots(spellLevel)[spellSlot];
                if (!spell.IsPopulated) continue;

                NwSpell? currentSpell = NwSpell.FromSpellId(spell.SpellId);

                if (currentSpell == null)
                {
                    Log.Info(message: "Spell is not a valid spell.");
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