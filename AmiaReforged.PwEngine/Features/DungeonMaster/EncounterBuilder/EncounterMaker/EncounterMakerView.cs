using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.EncounterBuilder.EncounterMaker;

public sealed class EncounterMakerView : ScryView<EncounterMakerPresenter>, IDmWindow
{
    public readonly NuiBind<string> Name = new(key: "search_val");

    public NuiButton OkButton = null!;

    public EncounterMakerView(NwPlayer player)
    {
        Presenter = new EncounterMakerPresenter(this, player);
    }

    public override EncounterMakerPresenter Presenter { get; protected set; }
    public string Title => "Create New Encounter";

    /// <summary>
    /// Is a child window of <see cref="EncounterBuilderPresenter"/>
    /// </summary>
    public bool ListInDmTools => false;

    public override NuiLayout RootLayout()
    {
        return new NuiColumn()
        {
            Children =
            [
                new NuiRow()
                {
                    Children =
                    [
                        new NuiTextEdit(label: "Name your encounter...", Name, 255, false),
                    ]
                },
                new NuiRow()
                {
                    Children =
                    [
                        new NuiSpacer(),
                        new NuiSpacer(),
                        new NuiButton("OK")
                        {
                            Id = "btn_ok",
                        }.Assign(out OkButton)
                    ]
                }
            ]
        };
    }

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;
}
