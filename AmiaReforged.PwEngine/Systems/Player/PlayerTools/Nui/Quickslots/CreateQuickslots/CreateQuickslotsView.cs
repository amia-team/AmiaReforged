using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using NuiUtils = AmiaReforged.PwEngine.Systems.WindowingSystem.NuiUtils;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Quickslots.CreateQuickslots;

public class CreateQuickslotsView : ScryView<CreateQuickSlotsPresenter>, IToolWindow
{
    public string Id => "playertools.quickslotscreate";
    public string Title => "Create Saved Quickslots";
    public string CategoryTag { get; } = null!;
    public bool RequiresPersistedCharacter { get; }

    public IScryPresenter MakeWindow(NwPlayer player)
    {
        return Presenter;
    }

    public bool ListInPlayerTools => false;
    public NuiWindow? WindowTemplate { get; }

    

    public readonly NuiBind<string> QuickslotName = new("quickslot_name");

    public NuiButton CreateButton = null!;
    public NuiButton CancelButton = null!;

    public CreateQuickslotsView(NwPlayer player)
    {
        Presenter = new CreateQuickSlotsPresenter(this, player);
    }

    public sealed override CreateQuickSlotsPresenter Presenter { get; protected set; }
    public override NuiLayout RootLayout()
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
                        NuiUtils.Assign(new NuiButton("Create") { Id = "create_quickslot_db" }, out CreateButton),
                        NuiUtils.Assign(new NuiButton("Cancel") { Id = "cancel_quickslot_db" }, out CancelButton)
                    }
                }
            }
        };
        return root;
    }
}