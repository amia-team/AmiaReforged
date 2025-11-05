using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.PlaceableEditor;

public sealed class PlaceableToolView : ScryView<PlaceableToolPresenter>, IToolWindow
{
    public NuiButton RecoverButton = null!;

    public PlaceableToolView(NwPlayer player)
    {
        Presenter = new PlaceableToolPresenter(this, player);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override PlaceableToolPresenter Presenter { get; protected set; }

    public string Id => "player.placeable.editor";
    public string Title => "Placeable Editor";
    public string CategoryTag => "World";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => false;

    public NuiButton RefreshButton = null!;
    public NuiButton SelectExistingButton = null!;
    public NuiButton SpawnButton = null!;

    public readonly NuiBind<int> BlueprintCount = new("plc_bp_count");
    public readonly NuiBind<string> BlueprintNames = new("plc_bp_names");
    public readonly NuiBind<string> BlueprintResRefs = new("plc_bp_resrefs");

    public readonly NuiBind<bool> SelectionAvailable = new("plc_selected_available");
    public readonly NuiBind<string> SelectedName = new("plc_selected_name");
    public readonly NuiBind<string> SelectedLocation = new("plc_selected_location");

    public readonly NuiBind<string> StatusMessage = new("plc_status_message");

    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> rowTemplate =
        [
            new(new NuiLabel(BlueprintNames)
            {
                VerticalAlign = NuiVAlign.Middle
            })
            {
                VariableSize = true
            },
            new(new NuiLabel(BlueprintResRefs)
            {
                VerticalAlign = NuiVAlign.Middle
            })
            {
                VariableSize = true
            },
            new(new NuiButton("Target Spawn")
            {
                Id = "btn_spawn",
                Height = 32f
            }.Assign(out SpawnButton))
            {
                VariableSize = false,
                Width = 110f
            }
        ];

        return new NuiColumn
        {
            Children =
            [
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiButton("Refresh Blueprints")
                        {
                            Id = "btn_refresh"
                        }.Assign(out RefreshButton),
                        new NuiButton("Select Existing")
                        {
                            Id = "btn_select"
                        }.Assign(out SelectExistingButton)
                    ]
                },
                new NuiList(rowTemplate, BlueprintCount)
                {
                    RowHeight = 36f,
                    Width = 0f,
                    Height = 260f
                },
                new NuiGroup
                {
                    Border = true,
                    Height = 100f,
                    Enabled = SelectionAvailable,
                    Element = new NuiColumn
                    {
                        Children =
                        [
                            new NuiLabel("Selected Placeable")
                            {
                                Height = 18f,
                                ForegroundColor = ColorConstants.White,
                                HorizontalAlign = NuiHAlign.Center
                            },
                            new NuiLabel(SelectedName)
                            {
                                Height = 18f,
                                HorizontalAlign = NuiHAlign.Center
                            },
                            new NuiLabel(SelectedLocation)
                            {
                                Height = 18f,
                                HorizontalAlign = NuiHAlign.Center
                            }
                        ]
                    }
                },
                new NuiButton("Recover Selected")
                {
                    Id = "btn_recover",
                    Height = 32f,
                    Enabled = SelectionAvailable
                }.Assign(out RecoverButton),
                new NuiLabel(StatusMessage)
                {
                    Height = 18f,
                    ForegroundColor = ColorConstants.Orange
                }
            ]
        };
    }

    public bool ShouldListForPlayer(NwPlayer player)
    {
        NwArea? area = player.ControlledCreature?.Area;
        if (area == null)
        {
            return false;
        }

        PlaceablePersistenceMode mode = area.GetPlaceablePersistenceMode();
        return mode != PlaceablePersistenceMode.None;
    }

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;
}
