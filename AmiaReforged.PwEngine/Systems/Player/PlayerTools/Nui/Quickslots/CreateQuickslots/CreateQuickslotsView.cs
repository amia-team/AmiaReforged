using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using NuiUtils = AmiaReforged.PwEngine.Systems.WindowingSystem.NuiUtils;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Quickslots.CreateQuickslots;

public class CreateQuickslotsView : ScryView<CreateQuickSlotsPresenter>, IToolWindow
{
    public readonly NuiBind<string> QuickslotName = new(key: "quickslot_name");
    public NuiButton CancelButton = null!;

    public NuiButton CreateButton = null!;

    public CreateQuickslotsView(NwPlayer player)
    {
        Presenter = new(this, player);
    }

    public NuiWindow? WindowTemplate { get; }

    public sealed override CreateQuickSlotsPresenter Presenter { get; protected set; }
    public string Id => "playertools.quickslotscreate";
    public string Title => "Create Saved Quickslots";
    public string CategoryTag { get; } = null!;
    public bool RequiresPersistedCharacter { get; }

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public bool ListInPlayerTools => false;

    public override NuiLayout RootLayout()
    {
        NuiColumn root = new()
        {
            Children =
            [
                new NuiRow
                {
                    Children =
                    [
                        new NuiLabel(label: "Quickslot Name")
                        {
                            Aspect = 2f
                        },

                        new NuiTextEdit(label: "Enter a Name", QuickslotName, 255, false)
                    ]
                },

                new NuiRow
                {
                    Children =
                    [
                        NuiUtils.Assign(new(label: "Create") { Id = "create_quickslot_db" }, out CreateButton),
                        NuiUtils.Assign(new(label: "Cancel") { Id = "cancel_quickslot_db" }, out CancelButton)
                    ]
                }
            ]
        };
        return root;
    }
}