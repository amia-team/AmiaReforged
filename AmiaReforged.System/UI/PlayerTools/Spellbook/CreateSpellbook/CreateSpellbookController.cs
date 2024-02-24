using System.Text.Json;
using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NLog.Fluent;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.Spellbook.CreateSpellbook;

public class CreateSpellbookController : WindowController<CreateSpellbookView>
{
    [Inject] private Lazy<SpellbookLoaderService> SpellbookLoader { get; set; }
    [Inject] private Lazy<CharacterService> CharacterService { get; set; }

    [Inject] private Lazy<WindowManager> WindowManager { get; set; }


    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public override void Init()
    {
        List<string> classnames = (from creatureClass in Token.Player.LoginCreature?.Classes
            where creatureClass.Class.IsSpellCaster
            select creatureClass.Class.Name.ToString()).ToList();
        List<NuiComboEntry> classnamesCombo = classnames.Select((t, i) => new NuiComboEntry(t, i)).ToList();
        Dictionary<int, string> classNameDict = new();
        for (int i = 0; i < classnames.Count; i++)
        {
            classNameDict.Add(i, classnames[i]);
        }

        Token.SetBindValue(View.ClassNames, classnamesCombo);
        Token.SetBindValue(View.Selection, classnamesCombo[0].Value);
        Token.SetBindValue(View.SpellbookName, string.Empty);
        Token.SetUserData(classNameDict);
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

    private async void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.ElementId == View.CreateButton.Id)
        {
            if (Token.GetBindValue(View.SpellbookName) == string.Empty)
            {
                Token.Player.SendServerMessage("You must enter a name for the spellbook.", ColorConstants.Red);
                return;
            }
            
            await SaveSpellbookToDb();
            await NwTask.SwitchToMainThread();

            Token.SetBindValue(View.SpellbookName, string.Empty);
            WindowManager.Value.OpenWindow<SpellbookListView>(Token.Player);
            Token.Close();
        }
        else if (eventData.ElementId == View.CancelButton.Id)
        {
            Token.SetBindValue(View.SpellbookName, string.Empty);
            Token.Close();
        }
    }

    private async Task SaveSpellbookToDb()
    {
        string spellbookName = Token.GetBindValue(View.SpellbookName);
        // Save spellbook to database
        string pcIdString = NWScript.GetLocalString(Token.Player.LoginCreature, "pc_guid");
        Guid pcId = Guid.Parse(pcIdString);

        bool characterExists = await CharacterService.Value.CharacterExists(pcId);
        await NwTask.SwitchToMainThread();

        // TODO: Refactor whatever the fuck this mess is supposed to be.
        if (spellbookName != null && pcId != Guid.Empty && characterExists)
        {
            int selectedClassIndex = Token.GetBindValue(View.Selection);
            Log.Info($"Selected class index: {selectedClassIndex}");

            Dictionary<int, string>? classNamesDict = Token.GetUserData<Dictionary<int, string>>();

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

                CreatureClassInfo creatureClassInfo = Token.Player.LoginCreature.Classes
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
                        }

                        preparedSpellsForLevel.Add(preparedSpell);
                    }

                    preparedSpells.TryAdd(i, preparedSpellsForLevel);
                }

                int classId = Token.Player.LoginCreature.Classes.ToList()
                    .Find(c => c.Class.Name.ToString() == selectedName)!.Class.Id;

                Log.Info($"Found classId: {classId}");

                SavedSpellbook savedSpellbook = new SavedSpellbook
                {
                    Id = Guid.NewGuid(),
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
                Token.Player.SendServerMessage(
                    "ERROR: Could not resolve class name for spellbook. Screenshot this and report it to the team.",
                    ColorConstants.Red);
            }
        }
    }

    protected override void OnClose()
    {
        Token.SetBindValue(View.SpellbookName, string.Empty);
    }
}