using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.DungeonMaster.EncounterBuilder;

public class EncounterBuilderView : ScryView<EncounterBuilderPresenter>, IDmWindow
{

    public EncounterBuilderView(NwPlayer player)
    {
        Presenter = new(this, player);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }
    public sealed override EncounterBuilderPresenter Presenter { get; protected set; }

    public readonly NuiBind<string> EncounterNames = new("enc_names");

    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> cells =
        [
            new(new NuiLabel(EncounterNames)),
            new(new NuiButton("X")
            {
                Id = "btn_delete",
                Aspect = 1f,
                Tooltip = "Delete Encounter",
            }),
        ];
        
        NuiColumn rootLayout = new() { };
        return rootLayout;
    }

    public string Title => "Encounter Builder";
    public bool ListInDmTools => true;
    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;
}