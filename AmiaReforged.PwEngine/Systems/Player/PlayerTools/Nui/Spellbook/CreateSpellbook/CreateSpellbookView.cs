﻿using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using NuiUtils = AmiaReforged.PwEngine.Systems.WindowingSystem.NuiUtils;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Spellbook.CreateSpellbook;

public class CreateSpellbookView : ScryView<CreateSpellbookPresenter>, IToolWindow
{
    public  string Id => "playertools.createspellbook";
    public  string Title => "Create Spellbook";
    public string CategoryTag { get; }
    public IScryPresenter MakeWindow(NwPlayer player)
    {
        throw new NotImplementedException();
    }

    public  bool ListInPlayerTools => false;



    public readonly NuiBind<string> SpellbookName = new("spellbook_name");
    public readonly NuiBind<int> Selection = new("selected_class1");

    public readonly NuiBind<List<NuiComboEntry>> ClassNames = new("class_names");

    public NuiButton CreateButton;
    public NuiCombo ClassComboBox;
    public NuiButton CancelButton;


    public CreateSpellbookView(NwPlayer player)
    {
        Presenter = new CreateSpellbookPresenter(this, player);
    }

    public override CreateSpellbookPresenter Presenter { get; protected set; }
    public override NuiLayout RootLayout()
    {
        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                NuiUtils.Assign(new NuiCombo()
                {
                    Entries = ClassNames,
                    Selected = Selection,
                }, out ClassComboBox),
                new NuiSpacer(),
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiLabel("Spellbook Name")
                        {
                            Aspect = 2f
                        },
                        new NuiTextEdit("Enter a Name", SpellbookName, 255, false)
                    }
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        NuiUtils.Assign(new NuiButton("Create") { Id = "create_spellbook_db" }, out CreateButton),
                        NuiUtils.Assign(new NuiButton("Cancel"), out CancelButton)
                    }
                }
            }
        };

        return root;
    }
}