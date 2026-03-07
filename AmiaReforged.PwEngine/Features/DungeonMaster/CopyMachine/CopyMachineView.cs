using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.CopyMachine;

public sealed class CopyMachineView : ScryView<CopyMachinePresenter>, IDmWindow
{
    private const float WindowW = 350f;
    private const float WindowH = 220f;

    public readonly NuiBind<string> StatusText = new("cm_status");

    public NuiButton SelectSourceButton = null!;
    public NuiButton CopyToTargetButton = null!;

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
                    Children =
                    [
                        new NuiLabel(StatusText)
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Center
                        }
                    ]
                },

                // Spacer
                new NuiSpacer { Height = 10f },

                // Select Source button
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiButton("Select Object to Copy")
                        {
                            Id = "btn_source"
                        }.Assign(out SelectSourceButton)
                    ]
                },

                // Spacer
                new NuiSpacer { Height = 10f },

                // Copy to Target button
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiButton("Select Object to Overwrite")
                        {
                            Id = "btn_target"
                        }.Assign(out CopyToTargetButton)
                    ]
                }
            ]
        };

        return root;
    }
}


