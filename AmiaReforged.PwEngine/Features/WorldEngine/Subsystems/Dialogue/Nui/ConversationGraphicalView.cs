using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Nui;

/// <summary>Dialogue artwork and live binds, shared by the visual prototype and future conversation shell.</summary>
public sealed class ConversationGraphicalView : IScryView
{
    public const int MaxVisibleChoices = 5;
    public const float BaseWindowW = 885f;
    public const float BaseWindowH = 762f;
    private const float ArtworkScale = 0.75f;
    public static readonly Color Gold = new(242, 196, 113);
    public static readonly Color Muted = new(110, 100, 80);
    private float _scaleFactor = 1f;

    public readonly NuiBind<string> SpeakerName = new("conv_speaker_name");
    public readonly NuiBind<string> NpcPortrait = new("conv_portrait");
    public readonly NuiBind<string> NpcText = new("conv_npc_text");
    public readonly NuiBind<string> TextPageInfo = new("conv_text_page_info");
    public readonly NuiBind<bool> ShowPrevTextPage = new("conv_show_prev_text");
    public readonly NuiBind<bool> ShowNextTextPage = new("conv_show_next_text");
    public readonly NuiBind<bool> ShowTextPagination = new("conv_show_text_pag");
    public readonly List<NuiBind<string>> ChoiceTexts = [];
    public readonly List<NuiBind<bool>> ChoiceVisible = [];
    public readonly NuiBind<bool> ShowMoreButton = new("conv_show_more");
    public readonly NuiBind<string> MoreButtonText = new("conv_more_text");
    public readonly NuiBind<string> GoodbyeText = new("conv_goodbye_text");
    public readonly NuiBind<Color> PreviousColor = new("conv_previous_color");
    public readonly NuiBind<Color> NextColor = new("conv_next_color");
    public readonly HashSet<string> ImageActionIds = [];

    public ConversationGraphicalView()
    {
        for (int i = 0; i < MaxVisibleChoices; i++)
        {
            ChoiceTexts.Add(new NuiBind<string>($"conv_choice_{i}"));
            ChoiceVisible.Add(new NuiBind<bool>($"conv_choice_vis_{i}"));
        }
    }

    public void SetScaleFactor(float scaleFactor) => _scaleFactor = scaleFactor > 0 ? scaleFactor : 1f;
    private float S(float sourcePixels) => sourcePixels * ArtworkScale / _scaleFactor;
    public float WindowWidth => BaseWindowW / _scaleFactor;
    public float WindowHeight => BaseWindowH / _scaleFactor;

    public NuiLayout RootLayout()
    {
        ImageActionIds.Clear();
        // As in Codex, the root is a wrapper; draw the shell on its child widget.
        return Group("conv_root", 1180, 1016, new NuiColumn
        {
            Width = S(1180), Height = S(1016), Margin = 0, Padding = 0,
            Children = [BuildShell()]
        });
    }

    private NuiGroup BuildShell() => Group("conv_shell", 1180, 1016, new NuiColumn
    {
        Width = S(1180), Height = S(1016), Margin = 0, Padding = 0,
        Children =
        [
            Gap(22), BuildHeader(), Gap(19),
            Row(1180, 494,
                Space(45), Group("conv_portrait_slot", 335, 494, new NuiColumn
                {
                    Width = S(335), Height = S(494), Margin = 0, Padding = 0,
                    Children = [BuildPortrait(), Gap(23)]
                }), Space(17),
                Group("conv_panel_slot", 746, 494, new NuiColumn
                {
                    Width = S(746), Height = S(494), Margin = 0, Padding = 0,
                    Children = [Gap(3), BuildTextPanel()]
                }), Space(37)),
            Gap(15), BuildChoices(), Gap(11), BuildFooter(), Gap(31)
        ]
    }, [Picture("ui_cdx_bg", 12, 12, 1156, 992), .. Frame(1180, 1016, "fr", 112, 144),
        Picture("ui_dlg_rule", 22, 83, 1136, 2)]);

    private NuiRow BuildHeader() => Row(1180, 64,
        Space(235), Divider(70, 14), Space(10),
        Label(SpeakerName, 535, 64), Space(10), Divider(70, 14), Space(173),
        Image("conv_close", "ui_cdx_close", 58, 64, "Close conversation", aspect: NuiAspect.Fit), Space(19));

    private NuiGroup BuildPortrait() => Group("conv_portrait_frame", 335, 471,
        Inset(new NuiImage(NpcPortrait)
        {
            Id = "conv_portrait_image", Width = S(311), Height = S(443),
            Margin = 0, Padding = 0, ImageAspect = NuiAspect.Fit
        }, 335, 471, 12, 14),
        [Picture("ui_cdx_bg", 6, 6, 323, 459), .. Frame(335, 471, "pf", 48, order: NuiDrawListItemOrder.After)]);

    private NuiGroup BuildTextPanel() => Group("conv_text_panel", 746, 491,
        new NuiColumn
        {
            Width = S(746), Height = S(491), Margin = 0, Padding = 0,
            Children =
            [
                Gap(18), Row(746, 46, Space(34), Label(SpeakerName, 678, 46), Space(34)), Gap(18),
                Row(746, 344, Space(34), new NuiText(NpcText)
                {
                    Id = "conv_npc_text", Width = S(678), Height = S(344), Margin = 0, Padding = 0,
                    ForegroundColor = Gold, Border = false, Scrollbars = NuiScrollbars.Y, Scissor = true
                }, Space(34)),
                Gap(8), Row(746, 48, Space(34), BuildTextPagination(), Space(34)), Gap(9)
            ]
        }, [Picture("ui_cdx_bg", 6, 6, 734, 479), .. Frame(746, 491, "pn", 48),
            .. DividerPictures(206, 61, 336, 14)]);

    private NuiGroup BuildTextPagination() => Group("conv_text_pagination_slot", 678, 48,
        new NuiRow
        {
            Width = S(678), Height = S(48), Margin = 0, Padding = 0, Visible = ShowTextPagination,
            Children =
            [
                Space(55), Divider(112, 48, 12), Space(42),
                Image("btn_prev_text", "ui_cdx_btn", 64, 48, "Previous text page",
                    glyph: "ui_cdx_i_prev", enabled: ShowPrevTextPage, color: PreviousColor),
                Space(16), Label(TextPageInfo, 104, 48), Space(18),
                Image("btn_next_text", "ui_cdx_btn", 64, 48, "Next text page",
                    glyph: "ui_cdx_i_next", enabled: ShowNextTextPage, color: NextColor),
                Space(26), Divider(112, 48, 12), Space(65)
            ]
        });

    private NuiColumn BuildChoices()
    {
        List<NuiElement> children = [];
        for (int i = 0; i < MaxVisibleChoices; i++)
        {
            // Hide the image inside a fixed slot, never the slot itself.
            children.Add(Row(1180, 56, Space(46),
                Group($"conv_choice_slot_{i}", 1088, 56,
                    Image($"btn_choice_{i}", "ui_dlg_choice", 1088, 56, ChoiceTexts[i],
                        label: ChoiceTexts[i], visible: ChoiceVisible[i], labelInset: 36)), Space(46)));
            children.Add(Gap(4));
        }
        return new NuiColumn { Width = S(1180), Height = S(300), Margin = 0, Padding = 0, Children = children };
    }

    private NuiRow BuildFooter() => Row(1180, 60,
        Space(46), Image("btn_goodbye", "ui_dlg_footer", 268, 60, "End conversation",
            label: GoodbyeText, labelInset: 44), Space(548),
        Group("conv_more_slot", 268, 60,
            Image("btn_more", "ui_dlg_footer", 268, 60, "More responses",
                label: MoreButtonText, visible: ShowMoreButton, glyph: "ui_cdx_i_next", labelInset: 44)), Space(50));

    private NuiImage Image(string id, string texture, float width, float height, NuiProperty<string> tooltip,
        NuiProperty<string>? label = null, NuiProperty<bool>? visible = null, NuiProperty<bool>? enabled = null,
        string? glyph = null, NuiProperty<Color>? color = null, float labelInset = 12,
        NuiAspect aspect = NuiAspect.Stretch)
    {
        ImageActionIds.Add(id);
        List<NuiDrawListItem> draw = [];
        if (label != null)
        {
            // Native font size does not shrink with inverse GUI compensation.
            // Keep the label rectangle tall enough at 150/200% GUI scale.
            float labelHeight = Math.Min(24f, S(height - 8));
            draw.Add(new NuiDrawListText(Gold,
                new NuiRect(S(labelInset), (S(height) - labelHeight) / 2,
                    S(width - 2 * labelInset - (glyph == null ? 0 : 28)), labelHeight), label)
            { Order = NuiDrawListItemOrder.After });
        }
        if (glyph != null)
        {
            float x = label == null ? (width - 28) / 2 : width - 40;
            draw.Add(new NuiDrawListImage(glyph, new NuiRect(S(x), S((height - 28) / 2), S(28), S(28)))
            { Color = color ?? Gold, Aspect = NuiAspect.Fit, Order = NuiDrawListItemOrder.After });
        }
        return new NuiImage(texture)
        {
            Id = id, Width = S(width), Height = S(height), Margin = 0, Padding = 0,
            ImageAspect = aspect, Tooltip = tooltip, Scissor = true,
            Visible = visible ?? new NuiValue<bool>(true), Enabled = enabled ?? new NuiValue<bool>(true), DrawList = draw
        };
    }

    private NuiLabel Label(NuiProperty<string> text, float width, float height) => new(text)
    {
        Width = S(width), Height = S(height), Margin = 0, Padding = 0,
        ForegroundColor = Gold, HorizontalAlign = NuiHAlign.Center, VerticalAlign = NuiVAlign.Middle, Tooltip = text
    };

    private NuiGroup Divider(float width, float height, float artworkHeight = 14) =>
        Group(null, width, height, new NuiSpacer { Margin = 0, Padding = 0 },
            DividerPictures(0, (height - artworkHeight) / 2, width, artworkHeight));

    private List<NuiDrawListItem> DividerPictures(float x, float y, float width, float height)
    {
        float center = 36 * height / 14;
        float tail = (width - center) / 2;
        return [Picture("ui_dlg_div_l", x, y, tail, height),
            Picture("ui_dlg_div_m", x + tail, y, center, height),
            Picture("ui_dlg_div_r", x + tail + center, y, tail, height)];
    }

    private NuiGroup Group(string? id, float width, float height, NuiElement element,
        List<NuiDrawListItem>? draw = null) => new()
    {
        Id = id!, Width = S(width), Height = S(height), Margin = 0, Padding = 0,
        Border = false, Scrollbars = NuiScrollbars.None, Scissor = true, Element = element, DrawList = draw ?? []
    };

    private NuiRow Row(float width, float height, params NuiElement[] elements) => new()
    { Width = S(width), Height = S(height), Margin = 0, Padding = 0, Children = [.. elements] };
    private NuiSpacer Gap(float height) => new() { Height = S(height), Margin = 0, Padding = 0 };
    private NuiSpacer Space(float width) => new() { Width = S(width), Margin = 0, Padding = 0 };

    private NuiColumn Inset(NuiElement element, float width, float height, float x, float y) => new()
    {
        Width = S(width), Height = S(height), Margin = 0, Padding = 0,
        Children = [Gap(y), Row(width, height - 2 * y, Space(x), element, Space(x)), Gap(y)]
    };

    private List<NuiDrawListItem> Frame(float width, float height, string prefix, float corner,
        float topCenter = 0, NuiDrawListItemOrder order = NuiDrawListItemOrder.Before)
    {
        List<NuiDrawListItem> draw =
        [
            Picture($"ui_dlg_{prefix}_tl", 0, 0, corner, corner, order),
            Picture($"ui_dlg_{prefix}_tr", width - corner, 0, corner, corner, order),
            Picture($"ui_dlg_{prefix}_bl", 0, height - corner, corner, corner, order),
            Picture($"ui_dlg_{prefix}_br", width - corner, height - corner, corner, corner, order),
            Picture($"ui_dlg_{prefix}_bot", corner, height - corner, width - 2 * corner, corner, order),
            Picture($"ui_dlg_{prefix}_left", 0, corner, corner, height - 2 * corner, order),
            Picture($"ui_dlg_{prefix}_right", width - corner, corner, corner, height - 2 * corner, order)
        ];
        if (topCenter == 0)
            draw.Add(Picture($"ui_dlg_{prefix}_top", corner, 0, width - 2 * corner, corner, order));
        else
        {
            float left = (width - topCenter) / 2;
            draw.AddRange([Picture($"ui_dlg_{prefix}_top_l", corner, 0, left - corner, corner, order),
                Picture($"ui_dlg_{prefix}_top_m", left, 0, topCenter, corner, order),
                Picture($"ui_dlg_{prefix}_top_r", left + topCenter, 0, left - corner, corner, order)]);
        }
        return draw;
    }

    private NuiDrawListImage Picture(string texture, float x, float y, float width, float height,
        NuiDrawListItemOrder order = NuiDrawListItemOrder.Before) =>
        new(texture, new NuiRect(S(x), S(y), S(width), S(height))) { Aspect = NuiAspect.Stretch, Order = order };
}
