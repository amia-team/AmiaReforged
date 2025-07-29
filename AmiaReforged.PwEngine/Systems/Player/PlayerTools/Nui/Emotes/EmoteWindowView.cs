using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Emotes;

public class EmoteWindowView : ScryView<EmoteWindowPresenter>, IToolWindow
{
    public NuiButton InteractionEmotesButton = null!;
    public NuiButton MagicalEmotesButton = null!;
    public NuiButton MutualEmotesButton = null!;


    public NuiButton SocialEmotesButton = null!;
    public NuiBind<int> VisibleEmoteCount = new(key: "emote_count");

    public NuiBind<string> VisibleEmoteLabels = new(key: "emote_labels");
    public NuiBind<string> VisibleEmoteTooltip = new(key: "emote_tooltip");

    public EmoteWindowView(NwPlayer player)
    {
        Presenter = new EmoteWindowPresenter(this, player);
    }

    public sealed override EmoteWindowPresenter Presenter { get; protected set; }
    public string Id => "playertools.emotewindow";
    public bool ListInPlayerTools => false;
    public bool RequiresPersistedCharacter => false;
    public string Title => "Emotes";
    public string CategoryTag => "Character";

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> emoteCells =
        [
            new(new NuiRow
            {
                Children =
                {
                    new NuiLabel(VisibleEmoteLabels),
                    new NuiButtonImage(resRef: "ir_action")
                    {
                        Id = "emote_button",
                        Tooltip = VisibleEmoteTooltip,
                        Width = 45f,
                        Height = 45f,
                        Aspect = 1f
                    }
                }
            })
            {
                Width = 200f,
                VariableSize = false
            }
        ];

        NuiColumn root = new()
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiButton(label: "Social")
                        {
                            Id = "filter_social",
                            Tooltip = "Cheering, waving, nodding, bowing, etc."
                        }.Assign(out SocialEmotesButton),
                        new NuiButton(label: "Magical")
                        {
                            Id = "filter_magical",
                            Tooltip = "Conjure animations, VFX, etc."
                        }.Assign(out MagicalEmotesButton),
                        new NuiButton(label: "Interactions")
                        {
                            Id = "filter_interactions",
                            Tooltip = "Drinking, sitting, etc."
                        }.Assign(out InteractionEmotesButton),
                        new NuiButton(label: "Mutual")
                        {
                            Id = "filter_mutual",
                            Tooltip = "Animations that require two players."
                        }.Assign(out MutualEmotesButton)
                    }
                },
                new NuiList(emoteCells, VisibleEmoteCount)
            }
        };

        return root;
    }
}