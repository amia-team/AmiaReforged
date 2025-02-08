using AmiaReforged.PwEngine.Systems.WindowingSystem;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Quickslots;

public sealed class QuickslotSaverView : ScryView<QuickslotSaverPresenter>, IToolWindow
{
    public string Id => "playertools.quickslots";
    public bool ListInPlayerTools => true;
    public  string Title => "Quickbar Loadouts";
    public string CategoryTag { get; }
    public IScryPresenter MakeWindow(NwPlayer player)
    {
        return new QuickslotSaverPresenter(this, player);
    }

    public  NuiWindow? WindowTemplate { get; }



    // Value bindings
    public  NuiBind<string> Search = new("search_val");
    public  NuiBind<string> QuickslotNames = new("qs_names");
    public  NuiBind<string> QuickslotIds = new("qs_ids");
    public  NuiBind<int> QuickslotCount = new("quickslot_count");

    // Buttons
    public  NuiButtonImage SearchButton;
    public  NuiButtonImage ViewQuickslotsButton;
    public  NuiButtonImage DeleteQuickslotsButton;
    public  NuiButtonImage CreateQuickslotsButton;

    public QuickslotSaverView(NwPlayer player)
    {
        Presenter = new QuickslotSaverPresenter(this, player);
    }

    public override QuickslotSaverPresenter Presenter { get; protected set; }
    public override NuiLayout RootLayout()
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

        return root;
    }

    public bool RequiresPersistedCharacter => true;
}