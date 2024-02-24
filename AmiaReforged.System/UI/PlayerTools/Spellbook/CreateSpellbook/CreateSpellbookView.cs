using AmiaReforged.Core.UserInterface;
using Anvil.API;

namespace AmiaReforged.System.UI.PlayerTools.Spellbook.CreateSpellbook;

public class CreateSpellbookView : WindowView<CreateSpellbookView>
{
    public override string Id => "playertools.createspellbook";
    public override string Title => "Create Spellbook";

    public override bool ListInPlayerTools => false;

    public override IWindowController? CreateDefaultController(NwPlayer player)
    {
        return CreateController<CreateSpellbookController>(player);
    }

    public readonly NuiBind<string> SpellbookName = new("spellbook_name");
    public readonly NuiBind<int> Selection = new("selected_class1");

    public readonly NuiBind<List<NuiComboEntry>> ClassNames = new("class_names");

    public readonly NuiButton CreateButton;
    public readonly NuiButton CancelButton;
    public readonly NuiCombo ClassComboBox;

    public override NuiWindow? WindowTemplate { get; }

    public CreateSpellbookView()
    {
        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                new NuiCombo()
                {
                    Entries = ClassNames,
                    Selected = Selection,
                }.Assign(out ClassComboBox),
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
                        new NuiButton("Create") { Id = "create_spellbook_db" }.Assign(out CreateButton),
                        new NuiButton("Cancel").Assign(out CancelButton)
                    }
                }
            }
        };
        WindowTemplate = new NuiWindow(root, Title)
        {
            Geometry = new NuiRect(0, 0, 400, 300),
            Closable = true,
            Resizable = false
        };
    }
}