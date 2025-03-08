using AmiaReforged.PwEngine.Systems.WindowingSystem;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.BuffRemover;

public class BuffRemoverView : ScryView<BuffRemoverPresenter>, IToolWindow
{
    public readonly NuiBind<int> BuffCount = new(key: "buff_count");

    public readonly NuiBind<string> EffectLabels = new(key: "effect_labels");

    public NuiButton RemoveAllButton = null!;

    public BuffRemoverView(NwPlayer player)
    {
        Presenter = new(this, player);
    }

    public sealed override BuffRemoverPresenter Presenter { get; protected set; }

    public string Id => "playertools.buffremover";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => false;
    public string Title => "Buff Remover";
    public string CategoryTag => "Character";

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> buffs = new()
        {
            new(new NuiRow
            {
                Children =
                {
                    new NuiLabel(EffectLabels),
                    new NuiButton(label: "X")
                    {
                        Id = "remove_effect"
                    }
                }
            })
        };

        NuiColumn root = new()
        {
            Children =
            {
                new NuiList(buffs, BuffCount)
                {
                    Width = 400
                },
                new NuiButton(label: "Remove All")
                {
                    Id = "remove_all"
                }.Assign(out RemoveAllButton)
            }
        };

        return root;
    }
}