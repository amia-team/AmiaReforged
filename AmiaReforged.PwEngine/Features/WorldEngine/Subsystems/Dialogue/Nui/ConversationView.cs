using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Nui;

/// <summary>
/// NUI view for the in-game NPC conversation window.
/// Layout: NPC portrait + text panel (top), choice buttons (middle), goodbye/more (bottom).
/// Matches the mockup with paginated NPC text and configurable player responses.
/// </summary>
public sealed class ConversationView : ScryView<ConversationPresenter>
{
    public const float WindowW = 520f;
    public const float WindowH = 480f;

    public const float PortraitW = 128f;
    public const float PortraitH = 160f;

    public const int MaxVisibleChoices = 5;

    // ── NPC portrait and text binds ──
    public readonly NuiBind<string> NpcPortrait = new("conv_portrait");
    public readonly NuiBind<string> NpcText = new("conv_npc_text");

    // ── Text pagination binds ──
    public readonly NuiBind<string> TextPageInfo = new("conv_text_page_info");
    public readonly NuiBind<bool> ShowPrevTextPage = new("conv_show_prev_text");
    public readonly NuiBind<bool> ShowNextTextPage = new("conv_show_next_text");

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
        // Initialize per-slot binds
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

    public override NuiLayout RootLayout()
    {
        return new NuiColumn
        {
            Children =
            [
                // ── Top section: Portrait + NPC text ──
                new NuiRow
                {
                    Height = PortraitH + 20f,
                    Children =
                    [
                        // NPC Portrait (left)
                        new NuiGroup
                        {
                            Width = PortraitW + 8f,
                            Height = PortraitH + 8f,
                            Border = true,
                            Scrollbars = NuiScrollbars.None,
                            Element = new NuiImage(NpcPortrait)
                            {
                                ImageAspect = NuiAspect.ExactScaled,
                                Width = PortraitW,
                                Height = PortraitH
                            }
                        },

                        // NPC text panel (right)
                        new NuiGroup
                        {
                            Border = true,
                            Scrollbars = NuiScrollbars.Y,
                            Element = new NuiColumn
                            {
                                Children =
                                [
                                    new NuiText(NpcText)
                                    {
                                        Scrollbars = NuiScrollbars.None
                                    }
                                ]
                            }
                        }
                    ]
                },

                // ── Text pagination row ──
                new NuiRow
                {
                    Height = 30f,
                    Children =
                    [
                        new NuiSpacer(),
                        new NuiButton("<")
                        {
                            Id = "btn_prev_text",
                            Width = 30f,
                            Height = 26f,
                            Enabled = ShowPrevTextPage
                        },
                        new NuiLabel(TextPageInfo)
                        {
                            Width = 60f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiButton(">")
                        {
                            Id = "btn_next_text",
                            Width = 30f,
                            Height = 26f,
                            Enabled = ShowNextTextPage
                        },
                        new NuiSpacer()
                    ]
                },

                // ── Choice buttons ──
                BuildChoiceSection(),

                // ── Bottom bar: Goodbye + More ──
                new NuiRow
                {
                    Height = 36f,
                    Children =
                    [
                        new NuiButton(GoodbyeText)
                        {
                            Id = "btn_goodbye",
                            Width = 120f,
                            Height = 32f
                        },
                        new NuiSpacer(),
                        new NuiButton(MoreButtonText)
                        {
                            Id = "btn_more",
                            Width = 80f,
                            Height = 32f,
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
                Height = 36f,
                Visible = ChoiceVisible[i],
                Children =
                [
                    new NuiButton(ChoiceTexts[i])
                    {
                        Id = $"btn_choice_{i}",
                        Height = 32f
                    },
                    new NuiButton(">")
                    {
                        Id = $"btn_choice_go_{i}",
                        Width = 30f,
                        Height = 32f
                    }
                ]
            });
        }

        return new NuiColumn { Children = children };
    }
}
