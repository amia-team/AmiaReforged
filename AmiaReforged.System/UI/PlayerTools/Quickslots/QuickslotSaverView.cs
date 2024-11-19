using AmiaReforged.Core.UserInterface;
using Anvil.API;

namespace AmiaReforged.System.UI.PlayerTools.Quickslots;

public sealed class QuickslotSaverView : WindowView<QuickslotSaverView>
{
    public override string Id => "playertools.quickslots";
    public override string Title => "Quickbar Loadouts";
    public override NuiWindow? WindowTemplate { get; }

    public override IWindowController? CreateDefaultController(NwPlayer player)
    {
        return CreateController<QuickslotSaverController>(player);
    }

    // Value bindings
    public readonly NuiBind<string> Search = new("search_val");
    public readonly NuiBind<string> QuickslotNames = new("qs_names");
    public readonly NuiBind<string> QuickslotIds = new("qs_ids");
    public readonly NuiBind<int> QuickslotCount = new("quickslot_count");

    // Buttons
    public readonly NuiButtonImage SearchButton;
    public readonly NuiButtonImage ViewQuickslotsButton;
    public readonly NuiButtonImage DeleteQuickslotsButton;
    public readonly NuiButtonImage CreateQuickslotsButton;

    public QuickslotSaverView()
    {
        List<NuiListTemplateCell> rowTemplate = new()
        {
            new NuiListTemplateCell(new NuiButtonImage("ir_xability")
            {
                Id = "btn_viewslots",
                Aspect = 1f,
                Tooltip = "Choose Loadout",
            }.Assign(out ViewQuickslotsButton))
            {
                VariableSize = false,
                Width = 35f,
            },
            new NuiListTemplateCell(new NuiButtonImage("ir_tmp_spawn")
            {
                Id = "btn_deleteslots",
                Aspect = 1f,
                Tooltip = "Delete Loadout",
            }.Assign(out DeleteQuickslotsButton))
            {
                VariableSize = false,
                Width = 35f,
            },
            new NuiListTemplateCell(new NuiLabel(QuickslotNames)
            {
                VerticalAlign = NuiVAlign.Middle,
            }),
            new NuiListTemplateCell(new NuiLabel(QuickslotIds)
            {
                Visible = false,
                Aspect = 1f
            }),
        };

        NuiColumn root = new()
        {
            Children = new List<NuiElement>()
            {
                new NuiRow()
                {
                    Height = 40f,
                    Children = new List<NuiElement>()
                    {
                        new NuiButtonImage("ir_rage")
                        {
                            Id = "btn_createslots",
                            Aspect = 1f,
                            Tooltip = "Save your hotbar loadout",
                        }.Assign(out CreateQuickslotsButton),
                        new NuiTextEdit("Search quickslots...", Search, 255, false),
                        new NuiButtonImage("isk_search")
                        {
                            Id = "btn_search",
                            Aspect = 1f,
                            Tooltip = "Search for a quickslot configuration.",
                        }.Assign(out SearchButton),
                    }
                },
                new NuiList(rowTemplate, QuickslotCount)
                {
                    RowHeight = 35f
                }
            }
        };

        WindowTemplate = new NuiWindow(root, Title)
        {
            Geometry = new NuiRect(0f, 100f, 400f, 600f),
        };
    }
}