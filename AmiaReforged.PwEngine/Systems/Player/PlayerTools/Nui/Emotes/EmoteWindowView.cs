using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Emotes.EmoteDefinitions;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using JetBrains.Annotations;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Emotes;

public class EmoteWindowView : ScryView<EmoteWindowPresenter>, IToolWindow
{
    public sealed override EmoteWindowPresenter Presenter { get; protected set; }
    public string Id => "playertoosl.emotewindow";
    public bool ListInPlayerTools => false;
    public bool RequiresPersistedCharacter => false;
    public string Title => "Emotes";
    public string CategoryTag => "Character";


    public NuiButton SocialEmotesButton = null!;
    public NuiButton InteractionEmotesButton = null!;
    public NuiButton MagicalEmotesButton = null!;
    public NuiButton MutualEmotesButton = null!;

    public NuiBind<string> VisibleEmoteLabels = new("emote_labels");
    public NuiBind<string> VisibleEmoteTooltip = new("emote_tooltip");
    public NuiBind<int> VisibleEmoteCount = new("emote_count");

    public EmoteWindowView(NwPlayer player)
    {
        Presenter = new EmoteWindowPresenter(this, player);
    }

    public IScryPresenter ForPlayer(NwPlayer player)
    {
        return Presenter;
    }

    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> emoteCells = new()
        {
            new NuiListTemplateCell(new NuiRow()
            {
                Children =
                {
                    new NuiLabel(VisibleEmoteLabels),
                    new NuiButtonImage("ir_action")
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
        };

        NuiColumn root = new()
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiButton("Social")
                        {
                            Id = "filter_social",
                            Tooltip = "Cheering, waving, nodding, bowing, etc."
                        }.Assign(out SocialEmotesButton),
                        new NuiButton("Magical")
                        {
                            Id = "filter_magical",
                            Tooltip = "Conjure animations, VFX, etc."
                        }.Assign(out MagicalEmotesButton),
                        new NuiButton("Interactions")
                        {
                            Id = "filter_interactions",
                            Tooltip = "Drinking, sitting, etc."
                        }.Assign(out InteractionEmotesButton),
                        new NuiButton("Mutual")
                        {
                            Id = "filter_mutual",
                            Tooltip = "Animations that require two players."
                        }.Assign(out MutualEmotesButton)
                    },
                },
                new NuiList(emoteCells, VisibleEmoteCount)
            }
        };

        return root;
    }
}