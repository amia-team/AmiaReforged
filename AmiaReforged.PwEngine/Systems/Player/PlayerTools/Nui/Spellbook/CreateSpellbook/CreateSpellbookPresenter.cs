using System.Text.Json;
using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.Player.PlayerId;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Spellbook.CreateSpellbook;

public class CreateSpellbookPresenter : ScryPresenter<CreateSpellbookView>
{
    private readonly NwPlayer _player;
    [Inject] private Lazy<SpellbookLoaderService> SpellbookLoader { get; set; }
    [Inject] private Lazy<CharacterService> CharacterService { get; set; }
    [Inject] private Lazy<PlayerIdService> PlayerIdService { get; set; }

    private NuiWindowToken _token = default;
    private NuiWindow? _window;

    public CreateSpellbookPresenter(CreateSpellbookView toolView, NwPlayer player)
    {
        ToolView = toolView;
        _player = player;
    }

    public override NuiWindowToken Token()
    {
        return _token;
    }

    public override CreateSpellbookView ToolView { get; }

    public override void Create()
    {
        // Create the window if it's null.
        if (_window == null)
        {
            // Try to create the window if it doesn't exist.
            InitBefore();
        }

        // If the window wasn't created, then tell the user we screwed up.
        if (_window == null)
        {
            _player.SendServerMessage("The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);
        List<string> classnames = (from creatureClass in Token().Player.LoginCreature?.Classes
            where creatureClass.Class.IsSpellCaster
            select creatureClass.Class.Name.ToString()).ToList();
        List<NuiComboEntry> classnamesCombo = classnames.Select((t, i) => new NuiComboEntry(t, i)).ToList();
        Dictionary<int, string> classNameDict = new();
        for (int i = 0; i < classnames.Count; i++)
        {
            classNameDict.Add(i, classnames[i]);
        }

        Token().SetBindValue(ToolView.ClassNames, classnamesCombo);
        Token().SetBindValue(ToolView.Selection, classnamesCombo[0].Value);
        Token().SetBindValue(ToolView.SpellbookName, string.Empty);
        Token().SetUserData(classNameDict);
    }

    [Inject] private Lazy<WindowManager> WindowManager { get; set; }


    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public override void InitBefore()
    {
        _window = new NuiWindow(ToolView.RootLayout(), ToolView.Title)
        {
            Geometry = new NuiRect(0, 0, 400, 300),
            Closable = true,
            Resizable = false
        };
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        Log.Info("Handling NUI event");
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                Log.Info("Handling NUI click event");
                HandleButtonClick(eventData);
                break;
        }
    }

    private async void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        Log.Info($"{eventData.ElementId}");
        if (eventData.ElementId == ToolView.CreateButton.Id)
        {
            if (Token().GetBindValue(ToolView.SpellbookName) == string.Empty)
            {
                Token().Player.SendServerMessage("You must enter a name for the spellbook.", ColorConstants.Red);
                return;
            }

            await SaveSpellbookToDb();
            await NwTask.SwitchToMainThread();

            Token().SetBindValue(ToolView.SpellbookName, string.Empty);
            Token().Close();
        }
        else if (eventData.ElementId == ToolView.CancelButton.Id)
        {
            Token().SetBindValue(ToolView.SpellbookName, string.Empty);
            Token().Close();
        }
    }

    private async Task SaveSpellbookToDb()
    {
        string spellbookName = Token().GetBindValue(ToolView.SpellbookName);

        // Save spellbook to database
        NwCreature? character = Token().Player.LoginCreature;
        if (character == null)
        {
            Log.Error("Character is null.");
            return;
        }

        Guid pcId = PlayerIdService.Value.GetPlayerKey(Token().Player);

        Log.Info($"Got player ID: {pcId}");
        
        bool characterExists = await CharacterService.Value.CharacterExists(pcId);
        await NwTask.SwitchToMainThread();
        Log.Info($"Character exists: {characterExists}");
        // TODO: Refactor whatever the fuck this mess is supposed to be.
        if (spellbookName != null && pcId != Guid.Empty && characterExists)
        {
            int selectedClassIndex = Token().GetBindValue(ToolView.Selection);
            Log.Info($"Selected class index: {selectedClassIndex}");

            Dictionary<int, string>? classNamesDict = Token().GetUserData<Dictionary<int, string>>();

            string? selectedName = classNamesDict?[selectedClassIndex];

            if (selectedName != null)
            {
                Log.Info($"Selected Name: {selectedName}");
                ClassPreparedSpells classPreparedSpells = new ClassPreparedSpells
                {
                    Class = selectedName,
                    IsInnate = false,
                };

                Dictionary<byte, IReadOnlyList<PreparedSpellModel>> preparedSpells = new();

                CreatureClassInfo creatureClassInfo = character.Classes
                    .Where(c => c.Class.Name.ToString() == selectedName).FirstOrDefault();
                Log.Info($"Found selected class: {creatureClassInfo.Class.Name.ToString()}");
                for (byte i = 0; i <= 9; i++)
                {
                    List<PreparedSpellModel> preparedSpellsForLevel = new();
                    foreach (MemorizedSpellSlot spellSlot in creatureClassInfo.GetMemorizedSpellSlots(i))
                    {
                        PreparedSpellModel preparedSpell = new();

                        if (spellSlot.IsPopulated)
                        {
                            preparedSpell.SpellName = spellSlot.Spell.Name.ToString();
                            preparedSpell.SpellId = spellSlot.Spell.Id;
                            if (spellSlot.Spell.IconResRef != null)
                                preparedSpell.IconResRef = spellSlot.Spell.IconResRef;
                            preparedSpell.IsDomainSpell = spellSlot.IsDomainSpell;
                            preparedSpell.MetaMagic = spellSlot.MetaMagic;
                            preparedSpell.IsPopulated = true;
                        }
                        else
                        {
                            preparedSpell.IsPopulated = false;
                            preparedSpell.IconResRef = "ir_tmp_spawn";
                            preparedSpell.SpellName = "Empty Slot";
                        }

                        preparedSpellsForLevel.Add(preparedSpell);
                    }

                    preparedSpells.TryAdd(i, preparedSpellsForLevel);
                }

                int classId = character.Classes.ToList()
                    .Find(c => c.Class.Name.ToString() == selectedName)!.Class.Id;

                Log.Info($"Found classId: {classId}");

                SavedSpellbook savedSpellbook = new SavedSpellbook
                {
                    SpellbookName = spellbookName,
                    PlayerCharacterId = pcId,
                    ClassId = classId
                };

                Log.Info($"Saving spellbook for Character with ID: {pcId} and Class ID: {classId}");

                savedSpellbook.SpellbookJson = JsonSerializer.Serialize(preparedSpells);

                Log.Info($"Spellbook JSON: {savedSpellbook.SpellbookJson}");

                await SpellbookLoader.Value.SaveSpellbook(savedSpellbook);
                await NwTask.SwitchToMainThread();
                Log.Info("Done saving spells.");
            }
            else
            {
                Token().Player.SendServerMessage(
                    "ERROR: Could not resolve class name for spellbook. Screenshot this and report it to the team.",
                    ColorConstants.Red);
            }
        }
    }

    public override void Close()
    {
        Token().SetBindValue(ToolView.SpellbookName, string.Empty);
    }
}