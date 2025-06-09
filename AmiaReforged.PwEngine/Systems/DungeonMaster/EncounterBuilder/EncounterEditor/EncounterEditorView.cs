using AmiaReforged.Core.Models;
using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.DungeonMaster.EncounterBuilder.EncounterEditor;

public class EncounterEditorView : ScryView<EncounterEditorPresenter>, IDmWindow
{
    public readonly NuiBind<string> Search = new(key: "search_val");


    public readonly NuiBind<string> EntryNames = new("entry_names");
    public readonly NuiBind<string> EntryAmount = new("entry_amnt");
    public readonly NuiBind<int> EntryCount = new("entry_count");

    public NuiButton SaveButton = null!;
    public NuiButtonImage SpawnNpcButton = null!;
    
    public NuiButtonImage SearchButton = null!;
    public NuiButtonImage AddNpcButton = null!;
    public NuiButtonImage DeleteNpcButton = null!;
    

    public sealed override EncounterEditorPresenter Presenter { get; protected set; }
    public string Title => "Edit Encounter";
    public bool ListInDmTools => false;

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public EncounterEditorView(NwPlayer player)
    {
        Presenter = new(this, player);
    }

    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> entryCells =
        [
            new(new NuiLabel(EntryNames)
            {
                VerticalAlign = NuiVAlign.Middle
            }),
            new(new NuiButtonImage("ir_abort")
            {
                Id = "btn_delete",
                Aspect = 1f,
                Tooltip = "Delete this Entry (Permanent)"
            }.Assign(out DeleteNpcButton))
            {
                VariableSize = false,
                Width = 35f
            }
        ];

        NuiColumn rootLayout = new()
        {
            Children =
            [
                new NuiRow()
                {
                    Children =
                    [
                        new NuiTextEdit("Search Encounter...", Search, 255, false),
                        new NuiButtonImage(resRef: "isk_search")
                        {
                            Id = "btn_search",
                            Aspect = 1f,
                        }.Assign(out SearchButton),
                        new NuiButtonImage(resRef: "ir_move")
                        {
                            Id = "btn_addnpc",
                            Aspect = 1f,
                            Tooltip = "Add an NPC to this encounter"
                        }.Assign(out AddNpcButton)
                    ],
                },
                new NuiRow()
                {
                    Children =
                    [
                        new NuiList(entryCells, EntryCount)
                    ]
                }
            ]
        };
        return rootLayout;
    }
}