using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.BuffRemover;

public class BuffRemoverView : ScryView<BuffRemoverPresenter>, IToolWindow
{
    private const float WindowW = 670f;
    private const float WindowH = 520f;
    private const float HeaderW = 600f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 8f;
    private const float HeaderLeftPad = 25f;

    public readonly NuiBind<int> BuffCount = new(key: "buff_count");

    public readonly NuiBind<string> EffectLabels = new(key: "effect_labels");

    public NuiButton RemoveAllButton = null!;

    public BuffRemoverView(NwPlayer player)
    {
        Presenter = new BuffRemoverPresenter(this, player);
    }

    public sealed override BuffRemoverPresenter Presenter { get; protected set; }

    public string Id => "playertools.buffremover";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => false;
    public string Title => "Buff Remover";
    public string CategoryTag => "Character";

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    private NuiElement BuildHeaderOverlay()
    {
        return new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_header", new NuiRect(HeaderLeftPad, HeaderTopPad, HeaderW, HeaderH))]
        };
    }

    public override NuiLayout RootLayout()
    {
        NuiRow bgLayer = new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH))]
        };

        NuiElement headerOverlay = BuildHeaderOverlay();
        NuiSpacer headerSpacer = new NuiSpacer { Height = HeaderH + HeaderTopPad + 6f };

        List<NuiListTemplateCell> buffs =
        [
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
        ];

        NuiColumn root = new()
        {
            Width = WindowW,
            Height = WindowH,
            Children =
            {
                bgLayer,
                headerOverlay,
                headerSpacer,
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 280f },
                        new NuiLabel("Buff Remover")
                        {
                            Height = 20f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 180f },
                        new NuiList(buffs, BuffCount)
                        {
                            Width = 300,
                            Height = 250
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 250f },
                        new NuiButton(label: "Remove All")
                        {
                            Id = "remove_all"
                        }.Assign(out RemoveAllButton)
                    }
                }
            }
        };
        return root;
    }
}
