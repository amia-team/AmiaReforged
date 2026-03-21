using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.CopyMachine;

public sealed class CopyMachineView : ScryView<CopyMachinePresenter>, IDmWindow
{
    private const float WindowW = 260f;
    private const float WindowH = 150f;

    public readonly NuiBind<string> StatusText = new("cm_status");
    public readonly NuiBind<bool> CopyEquipmentEnabled = new("cm_equipment_enabled");
    public readonly NuiBind<bool> CopyEquipmentChecked = new("cm_equipment_checked");

    public NuiButtonImage SelectSourceButton = null!;
    public NuiButtonImage CopyToTargetButton = null!;
    public NuiCheck CopyEquipmentCheckbox = null!;

    public override CopyMachinePresenter Presenter { get; protected set; }

    public string Title => "Copy Machine";
    public bool ListInDmTools => true;

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public CopyMachineView(NwPlayer player)
    {
        Presenter = new CopyMachinePresenter(this, player);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override NuiLayout RootLayout()
    {
        NuiColumn root = new()
        {
            Children =
            [
                // Background layer
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    Children = new List<NuiElement>(),
                    DrawList =
                    [
                        new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH))
                    ]
                },

                // Status label
                new NuiRow
                {
                    Height = 40f,
                    Width = 250,
                    Children =
                    [
                        new NuiLabel(StatusText)
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },

                // Spacer
                new NuiSpacer { Height = 10f },

                // Select Source row with label, button, and equipment checkbox
                new NuiRow
                {
                    Height = 40f,
                    Width = 250,
                    Children =
                    [
                        new NuiLabel("Select Source:")
                        {
                            Width = 100f,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiButtonImage("nui_pick")
                        {
                            Id = "btn_source",
                            Width = 40f,
                            Height = 40f,
                            Tooltip = "Select object whose appearance you want to copy."
                        }.Assign(out SelectSourceButton),
                        new NuiSpacer { Width = 15f },
                        new NuiCheck("Copy equipment", CopyEquipmentChecked)
                        {
                            Id = "chk_equipment",
                            Enabled = CopyEquipmentEnabled
                        }.Assign(out CopyEquipmentCheckbox)
                    ]
                },

                // Spacer
                new NuiSpacer { Height = 10f },

                // Select Target row with label and button
                new NuiRow
                {
                    Height = 40f,
                    Width = 250,
                    Children =
                    [
                        new NuiLabel("Select Target:")
                        {
                            Width = 100f,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiButtonImage("nui_pick")
                        {
                            Id = "btn_target",
                            Width = 40f,
                            Height = 40f,
                            Tooltip = "Select object to receive copied appearance. (Must be the same object type.)"
                        }.Assign(out CopyToTargetButton)
                    ]
                }
            ]
        };

        return root;
    }
}


