using AmiaReforged.PwEngine.Systems.WindowingSystem;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using JetBrains.Annotations;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.BuffRemover;

public class BuffRemoverView : ScryView<BuffRemoverPresenter>, IToolWindow
{
    public sealed override BuffRemoverPresenter Presenter { get; protected set; }

    public NuiButton RemoveAllButton = null!;

    public readonly NuiBind<string> EffectLabels = new("effect_labels");
    public readonly NuiBind<int> BuffCount = new("buff_count");

    public string Id => "playertools.buffremover";
    public bool ListInPlayerTools => false;
    public bool RequiresPersistedCharacter => false;
    public string Title => "Buff Remover";
    public string CategoryTag => "Character";

    public BuffRemoverView(NwPlayer player)
    {
        Presenter = new BuffRemoverPresenter(this, player);
    }

    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> buffs = new()
        {
            new NuiListTemplateCell(new NuiRow()
            {
                Children =
                {
                    new NuiLabel(EffectLabels),
                    new NuiButton("X")
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
                    Width = 400,
                },
                new NuiButton("Remove All")
                {
                    Id = "remove_all"
                }.Assign(out RemoveAllButton)
            }
        };

        return root;
    }

    public IScryPresenter ForPlayer(NwPlayer player)
    {
        return Presenter;
    }
}