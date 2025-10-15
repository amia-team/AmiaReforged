using System.Globalization;
using System.Numerics;
using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.PlcEdit;

public sealed class PlcEditorView : ScryView<PlcEditorPresenter>, IDmWindow
{
    public override PlcEditorPresenter Presenter { get; protected set; }

    public readonly NuiBind<string> Name = new("name_val");
    public readonly NuiBind<string> Description = new("desc_val");

    public readonly NuiBind<string> PortraitResRef = new("port_ref");
    public readonly NuiBind<string> PortraitPreview = new("port_preview");

    public readonly NuiBind<bool> ValidObjectSelected = new("valid_obj");

    public readonly NuiBind<int> AppearanceValue = new("plc_appear");
    public readonly NuiBind<float> Scale = new("plc_scale");

    public readonly NuiBind<float> PositionX = new("plc_pos_x");
    public readonly NuiBind<float> PositionY = new("plc_pos_y");
    public readonly NuiBind<float> PositionZ = new("plc_pos_z");
    public readonly NuiBind<string> PositionXString = new("plc_pos_x_str");
    public readonly NuiBind<string> PositionYString = new("plc_pos_y_str");
    public readonly NuiBind<string> PositionZString = new("plc_pos_z_str");

    public readonly NuiBind<float> RotationX = new("plc_rot_x");
    public readonly NuiBind<float> RotationY = new("plc_rot_y");
    public readonly NuiBind<float> RotationZ = new("plc_rot_z");
    public readonly NuiBind<string> RotationXString = new("plc_rot_x_str");
    public readonly NuiBind<string> RotationYString = new("plc_rot_y_str");
    public readonly NuiBind<string> RotationZString = new("plc_rot_z_str");

    public readonly NuiBind<float> TransformX = new("plc_trans_x");
    public readonly NuiBind<float> TransformY = new("plc_trans_y");
    public readonly NuiBind<float> TransformZ = new("plc_trans_z");
    public readonly NuiBind<string> TransformXString = new("plc_trans_x_str");
    public readonly NuiBind<string> TransformYString = new("plc_trans_y_str");
    public readonly NuiBind<string> TransformZString = new("plc_trans_z_str");

    public NuiButton SelectPlcButton = null!;

    public NuiButton Step1Button = null!;
    public NuiButton Step01Button = null!;
    public NuiButton Step001Button = null!;

    public NuiButton DecrementXButton = null!;
    public NuiButton IncrementXButton = null!;

    public NuiButton DecrementYButton = null!;
    public NuiButton IncrementYButton = null!;

    public NuiButton DecrementZButton = null!;
    public NuiButton IncrementZButton = null!;


    public PlcEditorView(NwPlayer player)
    {
        Presenter = new PlcEditorPresenter(this, player);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override NuiLayout RootLayout()
    {
        return new NuiGroup
        {
            Element = new NuiColumn
            {
                Width = 300f,
                Children =
                [
                    new NuiGroup
                    {
                    },
                    new NuiButton("Select PLC (Nearest or Targeted")
                    {
                        Id = "btn_plc_select"
                    }.Assign(out SelectPlcButton),
                    new NuiLabel("Name:"),
                    new NuiTextEdit("Enter a name...", Name, 200, false)
                    {
                        Enabled = ValidObjectSelected
                    },
                    new NuiLabel("Portrait"),
                    new NuiTextEdit("PortraitResRef", PortraitResRef, 16, false)
                    {
                        Enabled = ValidObjectSelected
                    },
                    new NuiLabel("Description"),
                    new NuiGroup
                    {
                        Element = new NuiRow
                        {
                            Children =
                            [
                                new NuiTextEdit("Description . . .", Description, 5000, true)
                                {
                                    Enabled = ValidObjectSelected,
                                    Height = 200
                                }
                            ]
                        }
                    },
                    new NuiLabel("Area Position"),
                    new NuiLabel("Step Value"),
                    new NuiRow
                    {
                        Children =
                        [
                            new NuiButton("0.01")
                            {
                                Id = "btn_step_001",
                                Enabled = ValidObjectSelected,
                                Tooltip = "X,Y,Z Values will be increased/decreased by 0.01"
                            }.Assign(out Step001Button),
                            new NuiButton("0.1")
                            {
                                Id = "btn_step_01",
                                Enabled = ValidObjectSelected,
                                Tooltip = "X,Y,Z Values will be increased/decreased by 0.1"
                            }.Assign(out Step01Button),
                            new NuiButton("1.0")
                            {
                                Id = "btn_step_1",
                                Enabled = ValidObjectSelected,
                                Tooltip = "X,Y,Z Values will be increased/decreased by 1.0"
                            }.Assign(out Step1Button),
                        ],
                        Tooltip = "Useful for fine tuning the position of a PLC"
                    },
                    new NuiGroup
                    {
                        Element = new NuiRow
                        {
                            Children =
                            [
                                new NuiButton("<")
                                {
                                    Id = "btn_pos_x_dec",
                                    Aspect = 1f,
                                    Enabled = ValidObjectSelected
                                }.Assign(out DecrementXButton),
                                new NuiTextEdit("0", PositionXString, 10, false)
                                {
                                    Tooltip = "X Value. Must be a valid decimal number",
                                    Enabled = ValidObjectSelected
                                },
                                new NuiButton(">")
                                {
                                    Id = "btn_pos_x_inc",
                                    Aspect = 1f,
                                    Enabled = ValidObjectSelected
                                }.Assign(out IncrementXButton)
                            ]
                        }
                    },
                    new NuiGroup
                    {
                        Element = new NuiRow
                        {
                            Children =
                            [
                                new NuiButton("<")
                                {
                                    Id = "btn_pos_y_dec",
                                    Aspect = 1f,
                                    Enabled = ValidObjectSelected
                                }.Assign(out DecrementYButton),
                                new NuiTextEdit("0", PositionYString, 10, false)
                                {
                                    Tooltip = "Y Value. Must be a valid decimal number",
                                    Enabled = ValidObjectSelected
                                },
                                new NuiButton(">")
                                {
                                    Id = "btn_pos_y_inc",
                                    Aspect = 1f,
                                    Enabled = ValidObjectSelected
                                }.Assign(out IncrementYButton)
                            ]
                        }
                    },
                    new NuiGroup
                    {
                        Element = new NuiRow
                        {
                            Children =
                            [
                                new NuiButton("<")
                                {
                                    Id = "btn_pos_z_dec",
                                    Aspect = 1f,
                                    Enabled = ValidObjectSelected
                                }.Assign(out DecrementZButton),
                                new NuiTextEdit("0", PositionZString, 10, false)
                                {
                                    Tooltip = "Z Value. Must be a valid decimal number",
                                    Enabled = ValidObjectSelected
                                },
                                new NuiButton(">")
                                {
                                    Id = "btn_pos_z_inc",
                                    Aspect = 1f,
                                    Enabled = ValidObjectSelected
                                }.Assign(out IncrementZButton)
                            ]
                        }
                    },
                ]
            }
        };
    }

    public string Title => "PLC Editor";
    public bool ListInDmTools => true;

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;
}

public sealed class PlcEditorPresenter : ScryPresenter<PlcEditorView>
{
    public override PlcEditorView View { get; }

    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public override NuiWindowToken Token() => _token;

    private readonly PlcEditorModel _model;


    public PlcEditorPresenter(PlcEditorView plcEditorView, NwPlayer player)
    {
        View = plcEditorView;
        _player = player;

        _model = new PlcEditorModel(player);

        _model.OnNewSelection += UpdateFromSelection;
    }

    private void UpdateFromSelection()
    {
        BindNewValues();
        ToggleBindWatch(_model.Selected != null);
    }

    private void BindNewValues()
    {
        bool selectionAvailable = _model.Selected != null;
        Token().SetBindValue(View.ValidObjectSelected, selectionAvailable);
        if (_model.Selected is null) return;
        Token().SetBindValue(View.Name, _model.Selected.Name);
        Token().SetBindValue(View.Description, _model.Selected.Description);
        Token().SetBindValue(View.PortraitResRef, _model.Selected.PortraitResRef);
        Token().SetBindValue(View.PortraitPreview, _model.Selected.PortraitResRef + "l");
        int appearanceRowIndex = _model.Selected?.Appearance.RowIndex ?? 1;
        Token().SetBindValue(View.AppearanceValue, appearanceRowIndex);
        Token().SetBindValue(View.TransformX, _model.Selected!.VisualTransform.Translation.X);
        Token().SetBindValue(View.TransformY, _model.Selected.VisualTransform.Translation.Y);
        Token().SetBindValue(View.TransformZ, _model.Selected.VisualTransform.Translation.Z);
        Token().SetBindValue(View.RotationX, _model.Selected.VisualTransform.Rotation.X);
        Token().SetBindValue(View.RotationY, _model.Selected.VisualTransform.Rotation.Y);
        Token().SetBindValue(View.RotationZ, _model.Selected.VisualTransform.Rotation.Z);
        Token().SetBindValue(View.Scale, _model.Selected.VisualTransform.Scale);

        Token().SetBindValue(View.PositionX, _model.Selected.Position.X);
        Token().SetBindValue(View.PositionXString, _model.Selected.Position.X.ToString(CultureInfo.InvariantCulture));
        Token().SetBindValue(View.PositionY, _model.Selected.Position.Y);
        Token().SetBindValue(View.PositionYString, _model.Selected.Position.Y.ToString(CultureInfo.InvariantCulture));
        Token().SetBindValue(View.PositionZ, _model.Selected.Position.Z);
        Token().SetBindValue(View.PositionZString, _model.Selected.Position.Z.ToString(CultureInfo.InvariantCulture));
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(0f, 100f, 400f, 600f)
        };
    }

    public override void Create()
    {
        if (_window == null)
            // Try to create the window if it doesn't exist.
            InitBefore();

        // If the window wasn't created, then tell the user we screwed up.
        if (_window == null)
        {
            _player.SendServerMessage(
                message: "The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        Token().SetBindValue(View.ValidObjectSelected, _model.Selected != null);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                HandleButtonClick(eventData);
                break;
            case NuiEventType.Watch:
                ToggleBindWatch(false);
                SanitizeInputs();
                UpdatePlc();
                ToggleBindWatch(true);
                break;
        }
    }

    private static string SanitizeNumericString(string input)
    {
        return new string(input.Where(c => char.IsDigit(c) || c == '.').ToArray());
    }

    private void SanitizeInputs()
    {
        SanitizePositions();
        SanitizeTransforms();
    }

    private void SanitizePositions()
    {
        string? newPositionXString = Token().GetBindValue(View.PositionXString);
        string? newPositionYString = Token().GetBindValue(View.PositionYString);
        string? newPositionZString = Token().GetBindValue(View.PositionZString);

        if (newPositionXString is null || newPositionYString is null || newPositionZString is null)
        {
            return;
        }

        string sanitizedX = SanitizeNumericString(newPositionXString);
        string sanitizedY = SanitizeNumericString(newPositionYString);
        string sanitizedZ = SanitizeNumericString(newPositionZString);

        Token().SetBindValue(View.PositionXString, sanitizedX);
        Token().SetBindValue(View.PositionYString, sanitizedY);
        Token().SetBindValue(View.PositionZString, sanitizedZ);

        if (float.TryParse(sanitizedX, out float x))
        {
            Token().SetBindValue(View.PositionX, x);
        }

        if (float.TryParse(sanitizedY, out float y))
        {
            Token().SetBindValue(View.PositionY, y);
        }

        if (float.TryParse(sanitizedZ, out float z))
        {
            Token().SetBindValue(View.PositionZ, z);
        }
    }

    private void SanitizeTransforms()
    {
        string? newTransformXString = Token().GetBindValue(View.TransformXString);
        string? newTransformYString = Token().GetBindValue(View.TransformYString);
        string? newTransformZString = Token().GetBindValue(View.TransformZString);

        if (newTransformXString is null || newTransformYString is null || newTransformZString is null)
        {
            return;
        }

        string sanitizedX = SanitizeNumericString(newTransformXString);
        string sanitizedY = SanitizeNumericString(newTransformYString);
        string sanitizedZ = SanitizeNumericString(newTransformZString);

        Token().SetBindValue(View.TransformXString, sanitizedX);
        Token().SetBindValue(View.TransformYString, sanitizedY);
        Token().SetBindValue(View.TransformZString, sanitizedZ);

        if (float.TryParse(sanitizedX, out float x))
        {
            Token().SetBindValue(View.TransformX, x);
        }

        if (float.TryParse(sanitizedY, out float y))
        {
            Token().SetBindValue(View.TransformY, y);
        }

        if (float.TryParse(sanitizedZ, out float z))
        {
            Token().SetBindValue(View.TransformZ, z);
        }
    }

    private void ToggleBindWatch(bool b)
    {
        Token().SetBindWatch(View.Name, b);
        Token().SetBindWatch(View.Description, b);
        Token().SetBindWatch(View.PortraitResRef, b);
        Token().SetBindWatch(View.AppearanceValue, b);
        Token().SetBindWatch(View.RotationX, b);
        Token().SetBindWatch(View.RotationY, b);
        Token().SetBindWatch(View.RotationZ, b);
        Token().SetBindWatch(View.TransformX, b);
        Token().SetBindWatch(View.TransformY, b);
        Token().SetBindWatch(View.TransformZ, b);

        Token().SetBindWatch(View.Scale, b);
        Token().SetBindWatch(View.PositionX, b);
        Token().SetBindWatch(View.PositionXString, b);
        Token().SetBindWatch(View.PositionY, b);
        Token().SetBindWatch(View.PositionYString, b);
        Token().SetBindWatch(View.PositionZ, b);
        Token().SetBindWatch(View.PositionZString, b);
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.ElementId != View.SelectPlcButton.Id) return;

        _model.EnterTargetingMode();
    }

    private void UpdatePlc()
    {
        string? newName = Token().GetBindValue(View.Name);
        string? newDescription = Token().GetBindValue(View.Description);
        string? newPortraitResRef = Token().GetBindValue(View.PortraitResRef);
        int newAppearance = Token().GetBindValue(View.AppearanceValue);
        float newRotationX = Token().GetBindValue(View.RotationX);
        float newRotationY = Token().GetBindValue(View.RotationY);
        float newRotationZ = Token().GetBindValue(View.RotationZ);
        float newTransformX = Token().GetBindValue(View.TransformX);
        float newTransformY = Token().GetBindValue(View.TransformY);
        float newTransformZ = Token().GetBindValue(View.TransformZ);
        float newScale = Token().GetBindValue(View.Scale);

        string? newPositionXString = Token().GetBindValue(View.PositionXString);
        string? newPositionYString = Token().GetBindValue(View.PositionYString);
        string? newPositionZString = Token().GetBindValue(View.PositionZString);

        if (newPositionXString is null || newPositionYString is null || newPositionZString is null)
        {
            return;
        }

        float newPositionX = Token().GetBindValue(View.PositionX);
        float newPositionY = Token().GetBindValue(View.PositionY);
        float newPositionZ = Token().GetBindValue(View.PositionZ);

        if (newName is null || newDescription is null || newPortraitResRef is null)
        {
            return;
        }

        PlaceableData newData = new(
            newName,
            newDescription,
            new PlaceableTransformData(
                new Vector3
                {
                    X = newTransformX,
                    Y = newTransformY,
                    Z = newTransformZ
                },
                new Vector3
                {
                    X = newRotationX,
                    Y = newRotationY,
                    Z = newRotationZ
                },
                newScale),
            new PlaceableAppearanceData(
                newAppearance,
                newPortraitResRef
            ),
            new PlaceableAreaPositionData(new Vector3
            {
                X = newPositionX,
                Y = newPositionY,
                Z = newPositionZ
            })
        );

        _model.Update(newData);
    }

    public override void Close()
    {
    }
}

internal sealed class PlcEditorModel(NwPlayer player)
{
    public NwPlaceable? Selected { get; private set; }

    public delegate void OnNewSelectionHandler();

    public event OnNewSelectionHandler? OnNewSelection;

    public void Update(PlaceableData data)
    {
        if (Selected is null) return;
        if (PlaceableDataFactory.From(Selected) == data) return;

        Selected.Name = data.Name;
        Selected.Description = data.Description;
        Selected.PortraitResRef = data.Appearance.PortraitResRef;
        ObjectPlugin.SetAppearance(Selected, data.Appearance.Appearance);
        Selected.VisualTransform.Translation = data.Transform.Translation;
        Selected.VisualTransform.Rotation = data.Transform.Rotation;
        Selected.VisualTransform.Scale = data.Transform.Scale;

        Selected.Position = data.Position.Position;
    }

    public void EnterTargetingMode()
    {
        player.EnterTargetMode(StartPlcSelection,
            new TargetModeSettings
                { ValidTargets = ObjectTypes.Placeable | ObjectTypes.Tile });
    }

    private void StartPlcSelection(ModuleEvents.OnPlayerTarget obj)
    {
        if (player.LoginCreature is null) return;

        if (Selected != null)
        {
            RemoveSelectedVfx();
        }

        if (obj.TargetObject is NwPlaceable placeable)
        {
            Selected = placeable;
            OnNewSelection?.Invoke();

            return;
        }

        NwArea? area = player.LoginCreature.Area;
        if (area is null) return;

        Location location = Location.Create(area, obj.TargetPosition, 0);

        NwPlaceable? nwPlaceable = location.GetNearestObjectsByType<NwPlaceable>().FirstOrDefault();

        if (nwPlaceable is null)
        {
            player.SendServerMessage("No placeable found nearby.");
            return;
        }

        Selected = nwPlaceable;
        OnNewSelection?.Invoke();
    }

    private void RemoveSelectedVfx()
    {
        if (Selected is null) return;
        Effect? selectedVfx = Selected.ActiveEffects.FirstOrDefault(e => e.Tag == SelectedVfxTag);

        if (selectedVfx is null) return;

        Selected.RemoveEffect(selectedVfx);
    }

    private const string SelectedVfxTag = "plc_select_vfx";
}

internal record PlaceableData(
    string Name,
    string Description,
    PlaceableTransformData Transform,
    PlaceableAppearanceData Appearance,
    PlaceableAreaPositionData Position);

internal static class PlaceableDataFactory
{
    public static PlaceableData From(NwPlaceable placeable)
    {
        return new PlaceableData(
            placeable.Name,
            placeable.Description,
            new PlaceableTransformData(
                placeable.VisualTransform.Translation,
                placeable.VisualTransform.Rotation,
                placeable.VisualTransform.Scale),
            new PlaceableAppearanceData(
                placeable.Appearance.RowIndex,
                placeable.PortraitResRef),
            new PlaceableAreaPositionData(placeable.Position)
        );
    }
}

internal record PlaceableTransformData(Vector3 Translation, Vector3 Rotation, float Scale);

internal record PlaceableAppearanceData(int Appearance, string PortraitResRef);

internal record PlaceableAreaPositionData(Vector3 Position);
