using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.CharacterTools.CustomSummon;

public sealed class CustomSummonDmPresenter(CustomSummonDmView view, NwPlayer player, NwItem widget)
    : ScryPresenter<CustomSummonDmView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public override CustomSummonDmView View { get; } = view;

    private readonly CustomSummonModel _model = new(player, widget);
    private NuiWindowToken _token;

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
    }

    public override void Create()
    {
        NuiWindow window = new NuiWindow(View.RootLayout(), "Custom Summon Manager [DM]")
        {
            Geometry = new NuiRect(0f, 50f, 630f, 570f),
            Resizable = false
        };

        if (!player.TryCreateNuiWindow(window, out _token))
            return;

        InitializeBindValues();
    }

    public override void Close()
    {
        _token.Close();
    }

    private void InitializeBindValues()
    {
        // Enable all controls
        Token().SetBindValue(View.AlwaysEnabled, true);

        RefreshSummonList();
    }

    private void RefreshSummonList()
    {
        // Get summon data from model
        List<string> summonNames = _model.GetSummonNames();

        // Set bind values
        Token().SetBindValue(View.SummonCount, summonNames.Count);
        Token().SetBindValues(View.SummonNames, summonNames);
        Token().SetBindValue(View.SelectedSummonIndex, -1);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                HandleClickEvent(eventData);
                break;
        }
    }

    private void HandleClickEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.ElementId)
        {
            case "csdm_btn_select_summon":
                // User clicked on a summon in the list
                int selectedIndex = eventData.ArrayIndex;
                Log.Info($"Summon selected at index: {selectedIndex}");
                Token().SetBindValue(View.SelectedSummonIndex, selectedIndex);
                player.SendServerMessage($"Selected summon {selectedIndex + 1}. Click 'Remove' to delete it.", ColorConstants.Cyan);
                break;

            case "csdm_btn_add":
                StartAddingSummon();
                break;

            case "csdm_btn_remove":
                RemoveSelectedSummon();
                break;

            case "csdm_btn_close":
                Close();
                break;
        }
    }

    private void StartAddingSummon()
    {
        player.EnterTargetMode(OnCreatureTargeted);
        player.SendServerMessage("Select a cust_summon template creature to add to this widget.", ColorConstants.Cyan);
    }

    private void OnCreatureTargeted(ModuleEvents.OnPlayerTarget targetEvent)
    {
        if (targetEvent.TargetObject is not NwCreature targetCreature)
        {
            player.SendServerMessage("You must target a creature.", ColorConstants.Orange);
            return;
        }

        if (targetCreature.Tag != "cust_summon")
        {
            player.SendServerMessage("You must use one of the summon templates for this item. Select a creature with the 'cust_summon' tag.", ColorConstants.Orange);
            return;
        }

        // Store the summon as JSON
        string summonJson = NWScript.JsonDump(NWScript.ObjectToJson(targetCreature, 1));
        string summonName = targetCreature.Name;

        // Get current summon list
        List<string> summonNames = _model.GetSummonNames();
        List<string> summonJsons = _model.GetSummonJsons();

        // If this is the first summon, add the item property to make it usable
        bool isFirstSummon = summonNames.Count == 0;

        // Add new summon
        summonNames.Add(summonName);
        summonJsons.Add(summonJson);

        // Save back to widget
        _model.SetSummonNames(summonNames);
        _model.SetSummonJsons(summonJsons);
        _model.UpdateWidgetAppearance();

        // If this was the first summon, add the cast spell property
        if (isFirstSummon)
        {
            AddWidgetItemProperty(widget);
            Log.Info("Added cast spell item property to widget (first summon)");
        }

        player.SendServerMessage($"Added {summonName.ColorString(ColorConstants.Cyan)} to the widget.", ColorConstants.Green);
        Log.Info($"DM {player.PlayerName} added summon '{summonName}' to widget");

        // Refresh the list
        RefreshSummonList();
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

    private void RemoveSelectedSummon()
    {
        int selection = Token().GetBindValue(View.SelectedSummonIndex);

        if (selection < 0)
        {
            player.SendServerMessage("No summon selected. Click on a summon in the list first.", ColorConstants.Orange);
            return;
        }

        List<string> summonNames = _model.GetSummonNames();
        List<string> summonJsons = _model.GetSummonJsons();

        if (selection >= summonNames.Count)
        {
            player.SendServerMessage("Invalid selection.", ColorConstants.Orange);
            return;
        }

        string removedName = summonNames[selection];
        summonNames.RemoveAt(selection);
        summonJsons.RemoveAt(selection);

        // Save back to widget
        _model.SetSummonNames(summonNames);
        _model.SetSummonJsons(summonJsons);
        _model.UpdateWidgetAppearance();

        player.SendServerMessage($"Removed {removedName.ColorString(ColorConstants.Cyan)} from the widget.", ColorConstants.Green);
        Log.Info($"DM {player.PlayerName} removed summon '{removedName}' from widget");

        // Refresh the list
        RefreshSummonList();
    }
}

