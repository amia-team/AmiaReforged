using System.Globalization;
using System.Numerics;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.PlcEdit;

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
        if (_model.Selected == null) return;

        ToggleBindWatch(false);

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
        Token().SetBindValue(View.TransformXString, _model.Selected.VisualTransform.Translation.X.ToString(CultureInfo.InvariantCulture));
        Token().SetBindValue(View.TransformYString, _model.Selected.VisualTransform.Translation.Y.ToString(CultureInfo.InvariantCulture));
        Token().SetBindValue(View.TransformZString, _model.Selected.VisualTransform.Translation.Z.ToString(CultureInfo.InvariantCulture));
        Token().SetBindValue(View.RotationX, _model.Selected.VisualTransform.Rotation.X);
        Token().SetBindValue(View.RotationY, _model.Selected.VisualTransform.Rotation.Y);
        Token().SetBindValue(View.RotationZ, _model.Selected.VisualTransform.Rotation.Z);
        Token().SetBindValue(View.RotationXString, _model.Selected.VisualTransform.Rotation.X.ToString(CultureInfo.InvariantCulture));
        Token().SetBindValue(View.RotationYString, _model.Selected.VisualTransform.Rotation.Y.ToString(CultureInfo.InvariantCulture));
        Token().SetBindValue(View.RotationZString, _model.Selected.VisualTransform.Rotation.Z.ToString(CultureInfo.InvariantCulture));
        Token().SetBindValue(View.Scale, _model.Selected.VisualTransform.Scale);
        Token().SetBindValue(View.ScaleString, _model.Selected.VisualTransform.Scale.ToString(CultureInfo.InvariantCulture));

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
        SanitizeScale();
        SanitizeRotations();
    }

    private void SanitizeRotations()
    {
        string? newRotationXString = Token().GetBindValue(View.RotationXString);
        string? newRotationYString = Token().GetBindValue(View.RotationYString);
        string? newRotationZString = Token().GetBindValue(View.RotationZString);

        if (newRotationXString is null || newRotationYString is null || newRotationZString is null)
        {
            return;
        }

        string sanitizedX = SanitizeNumericString(newRotationXString);
        string sanitizedY = SanitizeNumericString(newRotationYString);
        string sanitizedZ = SanitizeNumericString(newRotationZString);

        Token().SetBindValue(View.RotationXString, sanitizedX);
        Token().SetBindValue(View.RotationYString, sanitizedY);
        Token().SetBindValue(View.RotationZString, sanitizedZ);

        if (float.TryParse(sanitizedX, out float x))
        {
            Token().SetBindValue(View.RotationX, x);
        }

        if (float.TryParse(sanitizedY, out float y))
        {
            Token().SetBindValue(View.RotationY, y);
        }

        if (float.TryParse(sanitizedZ, out float z))
        {
            Token().SetBindValue(View.RotationZ, z);
        }
    }

    private void SanitizeScale()
    {
        string? newScaleString = Token().GetBindValue(View.ScaleString);

        if (newScaleString is null)
        {
            return;
        }

        string sanitizedScale = SanitizeNumericString(newScaleString);
        Token().SetBindValue(View.ScaleString, sanitizedScale);

        if (float.TryParse(sanitizedScale, out float scale))
        {
            Token().SetBindValue(View.Scale, scale);
        }
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
        Token().SetBindWatch(View.RotationXString, b);
        Token().SetBindWatch(View.RotationY, b);
        Token().SetBindWatch(View.RotationYString, b);
        Token().SetBindWatch(View.RotationZ, b);
        Token().SetBindWatch(View.RotationZString, b);
        Token().SetBindWatch(View.TransformX, b);
        Token().SetBindWatch(View.TransformXString, b);
        Token().SetBindWatch(View.TransformY, b);
        Token().SetBindWatch(View.TransformYString, b);
        Token().SetBindWatch(View.TransformZ, b);
        Token().SetBindWatch(View.TransformZString, b);

        Token().SetBindWatch(View.Scale, b);
        Token().SetBindWatch(View.ScaleString, b);

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