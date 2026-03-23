using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Nui;

/// <summary>
/// NUI view for the in-game NPC conversation window.
/// Layout (top-to-bottom):
///   • NPC portrait (left) + scrollable NPC text panel (right)
///   • Text pagination controls (only when text overflows)
///   • Player response choice buttons (paginated, up to 5 visible)
///   • Footer: Goodbye (left) / More (right, when choices overflow)
///
/// All element sizes are divided by a GUI-scale factor so the window
/// appears at a consistent physical size regardless of the player's
/// NWN GUI-scale setting.
/// </summary>
public sealed class ConversationView : ScryView<ConversationPresenter>
{
    // ── Base dimensions at 100 % GUI scale ──
    public const float BaseWindowW = 700f;
    public const float BaseWindowH = 560f;
    public const float BaseWindowX = 60f;
    public const float BaseWindowY = 80f;

    private const float BasePortraitW = 128f;
    private const float BasePortraitH = 160f;
    private const float BasePortraitPad = 10f;

    private const float BaseTextPanelH = 200f;

    private const float BaseButtonH = 36f;
    private const float BaseRowH = 42f;
    private const float BasePaginationH = 32f;
    private const float BaseSpacing = 8f;
    private const float BaseGoodbyeW = 140f;
    private const float BaseMoreW = 110f;
    private const float BasePagBtnW = 34f;
    private const float BasePagLabelW = 70f;

    public const int MaxVisibleChoices = 5;

    // ── Computed scaled sizes (set before RootLayout is called) ──
    private float _sf = 1f; // scale factor

    private float S(float baseVal) => baseVal / _sf;

    // ── NPC portrait and text binds ──
    public readonly NuiBind<string> NpcPortrait = new("conv_portrait");
    public readonly NuiBind<string> NpcText = new("conv_npc_text");

    // ── Text pagination binds ──
    public readonly NuiBind<string> TextPageInfo = new("conv_text_page_info");
    public readonly NuiBind<bool> ShowPrevTextPage = new("conv_show_prev_text");
    public readonly NuiBind<bool> ShowNextTextPage = new("conv_show_next_text");
    public readonly NuiBind<bool> ShowTextPagination = new("conv_show_text_pag");

    // ── Choice button binds (fixed 5 slots) ──
    public readonly List<NuiBind<string>> ChoiceTexts = [];
    public readonly List<NuiBind<bool>> ChoiceVisible = [];

    // ── More choices pagination ──
    public readonly NuiBind<bool> ShowMoreButton = new("conv_show_more");
    public readonly NuiBind<string> MoreButtonText = new("conv_more_text");

    // ── Goodbye button ──
    public readonly NuiBind<string> GoodbyeText = new("conv_goodbye_text");

    public ConversationView(NwPlayer player, DialogueService dialogueService)
    {
        for (int i = 0; i < MaxVisibleChoices; i++)
        {
            ChoiceTexts.Add(new NuiBind<string>($"conv_choice_{i}"));
            ChoiceVisible.Add(new NuiBind<bool>($"conv_choice_vis_{i}"));
        }

        Presenter = new ConversationPresenter(this, player, dialogueService);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override ConversationPresenter Presenter { get; protected set; }

    /// <summary>
    /// Sets the GUI-scale factor. Call before <see cref="RootLayout"/>.
    /// </summary>
    public void SetScaleFactor(float scaleFactor)
    {
        _sf = scaleFactor > 0f ? scaleFactor : 1f;
    }

    public override NuiLayout RootLayout()
    {
        return new NuiColumn
        {
            Children =
            [
                // ── Top section: Portrait + NPC text side-by-side ──
                new NuiRow
                {
                    Height = S(BaseTextPanelH + BasePortraitPad),
                    Children =
                    [
                        // NPC Portrait (left, fixed width)
                        new NuiGroup
                        {
                            Width = S(BasePortraitW + BasePortraitPad),
                            Height = S(BasePortraitH + BasePortraitPad),
                            Border = true,
                            Scrollbars = NuiScrollbars.None,
                            Element = new NuiImage(NpcPortrait)
                            {
                                ImageAspect = NuiAspect.ExactScaled,
                                Width = S(BasePortraitW),
                                Height = S(BasePortraitH)
                            }
                        },

                        // Spacer between portrait and text
                        new NuiSpacer { Width = S(BaseSpacing) },

                        // NPC text panel (fills remaining width, scrollable)
                        new NuiGroup
                        {
                            Border = true,
                            Scrollbars = NuiScrollbars.Y,
                            Element = new NuiText(NpcText)
                            {
                                Scrollbars = NuiScrollbars.None
                            }
                        }
                    ]
                },

                // ── Text pagination row (hidden when single page) ──
                new NuiRow
                {
                    Height = S(BasePaginationH),
                    Visible = ShowTextPagination,
                    Children =
                    [
                        new NuiSpacer(),
                        new NuiButton("<")
                        {
                            Id = "btn_prev_text",
                            Width = S(BasePagBtnW),
                            Height = S(BasePaginationH - 4f),
                            Enabled = ShowPrevTextPage
                        },
                        new NuiLabel(TextPageInfo)
                        {
                            Width = S(BasePagLabelW),
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiButton(">")
                        {
                            Id = "btn_next_text",
                            Width = S(BasePagBtnW),
                            Height = S(BasePaginationH - 4f),
                            Enabled = ShowNextTextPage
                        },
                        new NuiSpacer()
                    ]
                },

                // Spacing
                new NuiSpacer { Height = S(BaseSpacing) },

                // ── Choice buttons ──
                BuildChoiceSection(),

                // Flex spacer pushes footer to the bottom
                new NuiSpacer(),

                // ── Footer: Goodbye + More ──
                new NuiRow
                {
                    Height = S(BaseRowH),
                    Children =
                    [
                        new NuiButton(GoodbyeText)
                        {
                            Id = "btn_goodbye",
                            Width = S(BaseGoodbyeW),
                            Height = S(BaseButtonH)
                        },
                        new NuiSpacer(),
                        new NuiButton(MoreButtonText)
                        {
                            Id = "btn_more",
                            Width = S(BaseMoreW),
                            Height = S(BaseButtonH),
                            Visible = ShowMoreButton
                        }
                    ]
                }
            ]
        };
    }

    private NuiColumn BuildChoiceSection()
    {
        List<NuiElement> children = [];

        for (int i = 0; i < MaxVisibleChoices; i++)
        {
            children.Add(new NuiRow
            {
                Height = S(BaseRowH),
                Visible = ChoiceVisible[i],
                Children =
                [
                    new NuiButton(ChoiceTexts[i])
                    {
                        Id = $"btn_choice_{i}",
                        Height = S(BaseButtonH)
                    }
                ]
            });
        }

        return new NuiColumn { Children = children };
    }
}
