using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Nui;

/// <summary>Trait artwork and live binds, shared by the preview and future production shell.</summary>
public sealed class TraitSelectionGraphicalView : IScryView
{
    public const int EntriesPerPage = 8;
    public const float BaseWindowW = 940f;
    private const float ArtworkScale = BaseWindowW / 1400f;
    public const float BaseWindowH = 1020f * ArtworkScale;
    public const float BaseCompactWindowH = 112f * ArtworkScale;
    public static readonly Color Gold = new(242, 196, 113);
    public static readonly Color Muted = new(110, 100, 80);
    public static readonly (string Label, string Id)[] Categories =
    [
        ("All", "all"), ("Background", "background"), ("Personality", "personality"),
        ("Physical", "physical"), ("Mental", "mental"), ("Social", "social"),
        ("Supernatur.", "supernatural"), ("Curse", "curse"), ("Blessing", "blessing")
    ];

    public readonly NuiBind<string> HeaderTitle = new("trait_header_title");
    public readonly NuiBind<string> DetailTitle = new("trait_detail_title");
    public readonly NuiBind<string> DetailBody = new("trait_detail_body");
    public readonly NuiBind<string> BudgetLabel = new("trait_budget");
    public readonly NuiBind<string> PageInfo = new("trait_page_info");
    public readonly NuiBind<bool> ShowPrevPage = new("trait_show_prev");
    public readonly NuiBind<bool> ShowNextPage = new("trait_show_next");
    public readonly NuiBind<bool> ShowSelectButton = new("trait_show_select");
    public readonly NuiBind<bool> ShowDeselectButton = new("trait_show_deselect");
    public readonly NuiBind<bool> ControlsEnabled = new("trait_controls_enabled");
    public readonly NuiBind<Color> ControlsColor = new("trait_controls_color");
    public readonly NuiBind<string> CollapseGlyph = new("trait_collapse_glyph");
    public readonly List<NuiBind<string>> EntryNames = [];
    public readonly List<NuiBind<string>> EntrySubtitles = [];
    public readonly List<NuiBind<string>> EntryTooltips = [];
    public readonly List<NuiBind<string>> EntryTextures = [];
    public readonly List<NuiBind<bool>> EntryRowVisible = [];
    public readonly List<NuiBind<string>> CategoryTextures = [];
    public readonly HashSet<string> ImageActionIds = [];
    private float _scaleFactor = 1f;

    public TraitSelectionGraphicalView()
    {
        for (int i = 0; i < EntriesPerPage; i++)
        {
            EntryNames.Add(new NuiBind<string>($"trait_name_{i}"));
            EntrySubtitles.Add(new NuiBind<string>($"trait_sub_{i}"));
            EntryTooltips.Add(new NuiBind<string>($"trait_tooltip_{i}"));
            EntryTextures.Add(new NuiBind<string>($"trait_texture_{i}"));
            EntryRowVisible.Add(new NuiBind<bool>($"trait_vis_{i}"));
        }
        foreach (var (_, id) in Categories)
            CategoryTextures.Add(new NuiBind<string>($"trait_cat_texture_{id}"));
    }

    public void SetScaleFactor(float scaleFactor) => _scaleFactor = scaleFactor > 0 ? scaleFactor : 1f;
    private float S(float sourcePixels) => sourcePixels * ArtworkScale / _scaleFactor;
    public float WindowWidth => BaseWindowW / _scaleFactor;
    public float WindowHeight(bool compact) => (compact ? BaseCompactWindowH : BaseWindowH) / _scaleFactor;
    public NuiLayout RootLayout() => BuildLayout(false);

    public NuiLayout BuildLayout(bool compact, bool removeAction = false)
    {
        ImageActionIds.Clear();
        float height = compact ? 112 : 1020;
        List<NuiElement> content = [Gap(28), BuildHeader()];
        if (compact) content.Add(Gap(32));
        else content.AddRange([Gap(16), BuildBudget(), Gap(12), BuildPanels(removeAction), Gap(48)]);
        List<NuiDrawListItem> draw = [Picture("ui_cdx_bg", 22, 22, 1356, height - 44),
            .. Frame(1400, height, "fr", compact ? 48 : 80)];
        if (!compact) draw.Add(Picture("ui_dlg_rule", 18, 83, 1364, 2));
        NuiGroup shell = Group("trait_shell", 1400, height, Column(1400, height, content), draw);
        // The wrapper has no draw list; Codex renders shell artwork on a child group.
        return Group("trait_root", 1400, height, Column(1400, height, [shell]));
    }

    private NuiRow BuildHeader() => Row(1400, 52,
        Space(82), Center("trait_emblem_slot", 42, 52, new NuiImage("ui_cdx_i_compass")
        { Id = "trait_header_emblem", Width = S(42), Height = S(42), Margin = 0, Padding = 0, ImageAspect = NuiAspect.Fit }, 42),
        Space(16), Label(HeaderTitle, 900, 52, NuiHAlign.Left), Space(238),
        Center("trait_collapse_slot", 38, 52, Action("trait_collapse", "ui_cdx_btn", 38, 38,
            "Collapse or expand the preview header", glyph: CollapseGlyph, glyphSize: 20), 38),
        Space(18), Center("trait_close_slot", 42, 52,
            Action("btn_close", "ui_cdx_close", 42, 44, "Close trait selection", aspect: NuiAspect.Fit), 44), Space(24));

    private NuiRow BuildBudget() => Row(1400, 64, Space(54),
        Label(BudgetLabel, 1010, 64, NuiHAlign.Left), Space(24),
        Center("trait_confirm_slot", 268, 64,
            Action("btn_confirm", "ui_dlg_footer", 268, 60, "Confirm unconfirmed trait selections", label: "Confirm"), 60),
        Space(44));

    private NuiRow BuildPanels(bool removeAction) => Row(1400, 800,
        Space(36), BuildCategories(), Space(18), BuildEntryList(), Space(18), BuildDetail(removeAction), Space(36));

    private NuiGroup BuildCategories()
    {
        List<NuiElement> rows = [Gap(20), Row(244, 38, Space(18), Label("Categories", 208, 38), Space(18)), Gap(17)];
        const float buttonHeight = 214f * 72f / 262f;
        for (int i = 0; i < Categories.Length; i++)
        {
            (string label, string id) = Categories[i];
            string tooltip = id == "supernatural" ? "Show Supernatural traits" : $"Show {label} traits";
            rows.Add(Row(244, 74, Space(15),
                Group($"trait_category_slot_{id}", 214, 74,
                    Column(214, 74, [Action($"cat_{id}", CategoryTextures[i], 214, buttonHeight,
                        tooltip, label: label, labelInset: 22), Gap(74 - buttonHeight)])), Space(15)));
        }
        rows.Add(Gap(59));
        return Group("grp_trait_categories", 244, 800, Column(244, 800, rows),
            [.. Frame(244, 800, "pn", 48), .. DividerPictures(48, 58, 148, 10)]);
    }

    private NuiGroup BuildEntryList()
    {
        List<NuiElement> rows = [Gap(22)];
        for (int i = 0; i < EntriesPerPage; i++)
        {
            rows.Add(Row(468, 78, Space(14), Group($"trait_row_slot_{i}", 440, 78, Entry(i)), Space(14)));
            rows.Add(Gap(8));
        }
        rows.AddRange([Gap(37), BuildPagination(), Gap(10)]);
        return Group("trait_list_panel", 468, 800, Column(468, 800, rows), Frame(468, 800, "pn", 48));
    }

    private NuiImage Entry(int row)
    {
        ImageActionIds.Add($"btn_trait_{row}");
        float textHeight = Math.Min(24f, S(27));
        return new NuiImage(EntryTextures[row])
        {
            Id = $"btn_trait_{row}", Width = S(440), Height = S(78), Margin = 0, Padding = 0,
            ImageAspect = NuiAspect.Stretch, Visible = EntryRowVisible[row], Enabled = ControlsEnabled,
            Tooltip = EntryTooltips[row], Scissor = true,
            DrawList =
            [
                new NuiDrawListText(ControlsColor, new NuiRect(S(24), S(12), S(348), textHeight), EntryNames[row])
                { Order = NuiDrawListItemOrder.After },
                new NuiDrawListText(ControlsColor, new NuiRect(S(24), S(39), S(348), textHeight), EntrySubtitles[row])
                { Order = NuiDrawListItemOrder.After },
                Picture("ui_cdx_btn", 386, 17, 43, 43, NuiDrawListItemOrder.After),
                new NuiDrawListImage("ui_cdx_i_next", new NuiRect(S(397), S(27), S(22), S(22)))
                { Aspect = NuiAspect.Fit, Color = ControlsColor, Order = NuiDrawListItemOrder.After }
            ]
        };
    }

    private NuiRow BuildPagination() => Row(468, 43, Space(24),
        Group("trait_prev_slot", 43, 43, Action("btn_prev_page", "ui_cdx_btn", 43, 43,
            "Previous trait page", visible: ShowPrevPage, glyph: "ui_cdx_i_prev", glyphSize: 22)),
        Space(13), Divider(80, 43, 10), Space(8), Label(PageInfo, 132, 43), Space(8),
        Divider(80, 43, 10), Space(13),
        Group("trait_next_slot", 43, 43, Action("btn_next_page", "ui_cdx_btn", 43, 43,
            "Next trait page", visible: ShowNextPage, glyph: "ui_cdx_i_next", glyphSize: 22)), Space(24));

    private NuiGroup BuildDetail(bool removeAction) => Group("trait_detail_panel", 580, 800,
        Column(580, 800,
        [
            Gap(35), Row(580, 46, Space(46), Label(DetailTitle, 488, 46), Space(46)), Gap(27),
            Row(580, 596, Space(18), Group("trait_detail_text_frame", 544, 596,
                Column(544, 596, [Gap(22), Row(544, 552, Space(24), new NuiText(DetailBody)
                {
                    Id = "trait_detail_text", Width = S(496), Height = S(552), Margin = 0, Padding = 0,
                    Border = false, Scrollbars = NuiScrollbars.Y, Scissor = true, ForegroundColor = Gold
                }, Space(24)), Gap(22)]), Frame(544, 596, "pn", 32)), Space(18)),
            Gap(20), Row(580, 60, Space(156), Group("trait_detail_action_slot", 268, 60,
                Action(removeAction ? "btn_deselect_trait" : "btn_select_trait", "ui_dlg_footer", 268, 60,
                    removeAction ? "Remove this unconfirmed trait" : "Add this trait to your character",
                    label: removeAction ? "Remove" : "Select", visible: removeAction ? ShowDeselectButton : ShowSelectButton)), Space(156)),
            Gap(16)
        ]), [.. Frame(580, 800, "pn", 48), .. DividerPictures(156, 94, 270, 10)]);

    private NuiImage Action(string id, NuiProperty<string> texture, float width, float height,
        NuiProperty<string> tooltip, NuiProperty<string>? label = null, NuiProperty<bool>? visible = null,
        NuiProperty<string>? glyph = null, float glyphSize = 22, float labelInset = 44, NuiAspect aspect = NuiAspect.Stretch)
    {
        ImageActionIds.Add(id);
        List<NuiDrawListItem> draw = [];
        if (label != null)
        {
            float textHeight = Math.Min(24f, S(height - 8));
            draw.Add(new NuiDrawListText(ControlsColor, new NuiRect(S(labelInset), (S(height) - textHeight) / 2,
                S(width - labelInset * 2), textHeight), label) { Order = NuiDrawListItemOrder.After });
        }
        if (glyph != null)
            draw.Add(new NuiDrawListImage(glyph, new NuiRect(S((width - glyphSize) / 2), S((height - glyphSize) / 2),
                S(glyphSize), S(glyphSize)))
            { Aspect = NuiAspect.Fit, Color = ControlsColor, Order = NuiDrawListItemOrder.After });
        return new NuiImage(texture)
        {
            Id = id, Width = S(width), Height = S(height), Margin = 0, Padding = 0,
            ImageAspect = aspect, Tooltip = tooltip, Visible = visible ?? new NuiValue<bool>(true),
            Enabled = ControlsEnabled, DrawList = draw, Scissor = true
        };
    }

    private NuiLabel Label(NuiProperty<string> text, float width, float height, NuiHAlign alignment = NuiHAlign.Center) => new(text)
    {
        Width = S(width), Height = S(height), Margin = 0, Padding = 0, ForegroundColor = Gold,
        HorizontalAlign = alignment, VerticalAlign = NuiVAlign.Middle, Tooltip = text
    };

    private NuiGroup Center(string id, float width, float height, NuiElement child, float childHeight) =>
        Group(id, width, height, Column(width, height, [Gap((height - childHeight) / 2), child, Gap((height - childHeight) / 2)]));

    private NuiGroup Group(string id, float width, float height, NuiElement child, List<NuiDrawListItem>? draw = null) => new()
    {
        Id = id, Width = S(width), Height = S(height), Margin = 0, Padding = 0, Border = false,
        Scrollbars = NuiScrollbars.None, Scissor = true, Element = child, DrawList = draw ?? []
    };
    private NuiColumn Column(float width, float height, List<NuiElement> children) => new()
    { Width = S(width), Height = S(height), Margin = 0, Padding = 0, Children = children };
    private NuiRow Row(float width, float height, params NuiElement[] children) => new()
    { Width = S(width), Height = S(height), Margin = 0, Padding = 0, Children = [.. children] };
    private NuiSpacer Gap(float height) => new() { Height = S(height), Margin = 0, Padding = 0 };
    private NuiSpacer Space(float width) => new() { Width = S(width), Margin = 0, Padding = 0 };
    private NuiGroup Divider(float width, float height, float artworkHeight) =>
        Group("", width, height, new NuiSpacer { Margin = 0, Padding = 0 }, DividerPictures(0, (height - artworkHeight) / 2, width, artworkHeight));

    private List<NuiDrawListItem> DividerPictures(float x, float y, float width, float height)
    {
        float center = 36 * height / 14, tail = (width - center) / 2;
        return [Picture("ui_dlg_div_l", x, y, tail, height), Picture("ui_dlg_div_m", x + tail, y, center, height),
            Picture("ui_dlg_div_r", x + tail + center, y, tail, height)];
    }
    private List<NuiDrawListItem> Frame(float width, float height, string prefix, float corner) =>
    [
        Picture($"ui_trs_{prefix}_tl", 0, 0, corner, corner), Picture($"ui_trs_{prefix}_tr", width - corner, 0, corner, corner),
        Picture($"ui_trs_{prefix}_bl", 0, height - corner, corner, corner),
        Picture($"ui_trs_{prefix}_br", width - corner, height - corner, corner, corner),
        Picture($"ui_trs_{prefix}_top", corner, 0, width - 2 * corner, corner),
        Picture($"ui_trs_{prefix}_bot", corner, height - corner, width - 2 * corner, corner),
        Picture($"ui_trs_{prefix}_left", 0, corner, corner, height - 2 * corner),
        Picture($"ui_trs_{prefix}_right", width - corner, corner, corner, height - 2 * corner)
    ];
    private NuiDrawListImage Picture(string texture, float x, float y, float width, float height,
        NuiDrawListItemOrder order = NuiDrawListItemOrder.Before) =>
        new(texture, new NuiRect(S(x), S(y), S(width), S(height))) { Aspect = NuiAspect.Stretch, Order = order };
}
