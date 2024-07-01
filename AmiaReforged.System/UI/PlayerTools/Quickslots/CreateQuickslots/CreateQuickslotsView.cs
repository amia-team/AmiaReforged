using AmiaReforged.Core.UserInterface;
using Anvil.API;

namespace AmiaReforged.System.UI.PlayerTools.Quickslots.CreateQuickslots;

public class CreateQuickslotsView : WindowView<CreateQuickslotsView>
{
    public sealed override string Id => "playertools.quickslotscreate";
    public sealed override string Title => "Create Saved Quickslots";
    public override bool ListInPlayerTools => false;
    public override NuiWindow? WindowTemplate { get; }

    public override IWindowController? CreateDefaultController(NwPlayer player)
    {
        return CreateController<CreateQuickslotsController>(player);
    }

    public readonly NuiBind<string> QuickslotName = new("quickslot_name");

    public readonly NuiButton CreateButton;
    public readonly NuiButton CancelButton;

    public CreateQuickslotsView()
    {
        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                new NuiRow()
                {
                    Children = new List<NuiElement>
                    {
                        new NuiLabel("Quickslot Name")
                        {
                            Aspect = 2f
                        },
                        new NuiTextEdit("Enter a Name", QuickslotName, 255, false)
                    }
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiButton("Create") { Id = "create_quickslot_db" }.Assign(out CreateButton),
                        new NuiButton("Cancel") { Id = "cancel_quickslot_db" }.Assign(out CancelButton)
                    }
                }
            }
        };

        WindowTemplate = new NuiWindow(root, Title)
        {
            Geometry = new NuiRect(0, 0, 400, 300),
            Closable = true,
            Resizable = false,
            Collapsed = false,
        };
    }
}