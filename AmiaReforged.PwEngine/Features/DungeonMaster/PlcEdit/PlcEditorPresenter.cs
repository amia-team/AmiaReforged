using System.Globalization;
using System.Numerics;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Action = System.Action;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.PlcEdit;

public sealed class PlcEditorPresenter : ScryPresenter<PlcEditorView>
{
    public override PlcEditorView View { get; }

    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public override NuiWindowToken Token() => _token;

    private readonly PlcEditorModel _model;

    // Centralized blacklist for elementIds we want to ignore in Watch.
    private readonly HashSet<string> _watchBlacklist = new(StringComparer.OrdinalIgnoreCase);

    // Optionally debounce heavy updates. Keep interval modest to feel live but not spammy.
    private DateTime _lastApplyAt = DateTime.MinValue;
    private static readonly TimeSpan LiveApplyMinInterval = TimeSpan.FromMilliseconds(50);

    public PlcEditorPresenter(PlcEditorView plcEditorView, NwPlayer player)
    {
        View = plcEditorView;
        _player = player;

        _model = new PlcEditorModel(player);

        _model.OnNewSelection += UpdateFromSelection;
    }

    private void UpdateFromSelection()
    {
        UpdateFromModel();
    }

    private void BindNewValuesFromSelected()
    {
        bool selectionAvailable = _model.Selected != null;
        Token().SetBindValue(View.ValidObjectSelected, selectionAvailable);
        if (_model.Selected is null) return;

        Token().SetBindValue(View.Name, _model.Selected.Name);
        Token().SetBindValue(View.Description, _model.Selected.Description);
        Token().SetBindValue(View.PortraitResRef, _model.Selected.PortraitResRef);
        Token().SetBindValue(View.PortraitPreview, _model.Selected.PortraitResRef + "l");

        int appearanceRowIndex = _model.Selected.Appearance.RowIndex;
        Token().SetBindValue(View.AppearanceValue, appearanceRowIndex);
        Token().SetBindValue(View.AppearanceValueString, appearanceRowIndex.ToString(CultureInfo.InvariantCulture));

        Token().SetBindValue(View.TransformX, _model.Selected.VisualTransform.Translation.X);
        Token().SetBindValue(View.TransformY, _model.Selected.VisualTransform.Translation.Y);
        Token().SetBindValue(View.TransformZ, _model.Selected.VisualTransform.Translation.Z);
        Token().SetBindValue(View.TransformXString,
            _model.Selected.VisualTransform.Translation.X.ToString(CultureInfo.InvariantCulture));
        Token().SetBindValue(View.TransformYString,
            _model.Selected.VisualTransform.Translation.Y.ToString(CultureInfo.InvariantCulture));
        Token().SetBindValue(View.TransformZString,
            _model.Selected.VisualTransform.Translation.Z.ToString(CultureInfo.InvariantCulture));

        Token().SetBindValue(View.RotationX, _model.Selected.VisualTransform.Rotation.X);
        Token().SetBindValue(View.RotationY, _model.Selected.VisualTransform.Rotation.Y);
        Token().SetBindValue(View.RotationZ, _model.Selected.VisualTransform.Rotation.Z);
        Token().SetBindValue(View.RotationXString,
            _model.Selected.VisualTransform.Rotation.X.ToString(CultureInfo.InvariantCulture));
        Token().SetBindValue(View.RotationYString,
            _model.Selected.VisualTransform.Rotation.Y.ToString(CultureInfo.InvariantCulture));
        Token().SetBindValue(View.RotationZString,
            _model.Selected.VisualTransform.Rotation.Z.ToString(CultureInfo.InvariantCulture));

        Token().SetBindValue(View.Scale, _model.Selected.VisualTransform.Scale);
        Token().SetBindValue(View.ScaleString,
            _model.Selected.VisualTransform.Scale.ToString(CultureInfo.InvariantCulture));

        Token().SetBindValue(View.PositionX, _model.Selected.Position.X);
        Token().SetBindValue(View.PositionY, _model.Selected.Position.Y);
        Token().SetBindValue(View.PositionZ, _model.Selected.Position.Z);
        Token().SetBindValue(View.PositionXString, _model.Selected.Position.X.ToString(CultureInfo.InvariantCulture));
        Token().SetBindValue(View.PositionYString, _model.Selected.Position.Y.ToString(CultureInfo.InvariantCulture));
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
        UpdateFromModel();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                HandleButtonClick(eventData);
                break;
            case NuiEventType.Watch:
                // Mirror the reference pattern: when selection is null, refresh binds and exit.
                if (_model.Selected == null)
                {
                    UpdateFromModel();
                    return;
                }

                if (IsBlacklisted(eventData.ElementId))
                    return;

                ToggleBindWatch(false);
                HandleWatchUpdate(eventData.ElementId);
                ToggleBindWatch(true);
                break;
        }
    }


    private void HandleWatchUpdate(string? elementId)
    {
        if (string.IsNullOrEmpty(elementId)) return;

        // Check if this is a numeric slider bind (not the slider ID, but the bind key)
        if (IsNumericSliderBind(elementId))
        {
            DateTime now = DateTime.UtcNow;
            SyncNumericToString(elementId);
            if (now - _lastApplyAt >= LiveApplyMinInterval)
            {
                ApplyPlcSafe();
                _lastApplyAt = now;
            }
            return;
        }

        // Text fields that mirror floats: sanitize, push numeric, then apply.
        if (TryHandleNumericTextPair(elementId))
        {
            ApplyPlcSafe();
            return;
        }

        // Other simple fields (name/desc/portrait/appearance string): sanitize where needed, then apply.
        if (elementId.Equals(View.Name.Key, StringComparison.OrdinalIgnoreCase) ||
            elementId.Equals(View.Description.Key, StringComparison.OrdinalIgnoreCase) ||
            elementId.Equals(View.PortraitResRef.Key, StringComparison.OrdinalIgnoreCase))
        {
            ApplyPlcSafe();
            return;
        }

        if (elementId.Equals(View.AppearanceValueString.Key, StringComparison.OrdinalIgnoreCase))
        {
            string? s = Token().GetBindValue(View.AppearanceValueString) ?? string.Empty;
            string sanitized = SanitizeNumericString(s);
            Token().SetBindValue(View.AppearanceValueString, sanitized);
            if (int.TryParse(sanitized, out int idx))
            {
                idx = Math.Abs(idx);
                Token().SetBindValue(View.AppearanceValue, idx);
            }
            ApplyPlcSafe();
            return;
        }
    }

    private bool IsNumericSliderBind(string? elementId)
    {
        if (string.IsNullOrEmpty(elementId)) return false;

        return elementId.Equals(View.PositionX.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.PositionY.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.PositionZ.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.TransformX.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.TransformY.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.TransformZ.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.RotationX.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.RotationY.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.RotationZ.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.Scale.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.PositionStep.Key, StringComparison.OrdinalIgnoreCase);
    }

    private void SyncNumericToString(string? elementId)
    {
        if (string.IsNullOrEmpty(elementId)) return;

        void SyncFloat(NuiBind<float> valBind, NuiBind<string> strBind)
        {
            if (!elementId.Equals(valBind.Key, StringComparison.OrdinalIgnoreCase)) return;
            float v = Token().GetBindValue(valBind);
            Token().SetBindValue(strBind, v.ToString(CultureInfo.InvariantCulture));
        }

        SyncFloat(View.PositionX, View.PositionXString);
        SyncFloat(View.PositionY, View.PositionYString);
        SyncFloat(View.PositionZ, View.PositionZString);
        SyncFloat(View.TransformX, View.TransformXString);
        SyncFloat(View.TransformY, View.TransformYString);
        SyncFloat(View.TransformZ, View.TransformZString);
        SyncFloat(View.RotationX, View.RotationXString);
        SyncFloat(View.RotationY, View.RotationYString);
        SyncFloat(View.RotationZ, View.RotationZString);
        SyncFloat(View.Scale, View.ScaleString);
        SyncFloat(View.PositionStep, View.PositionStepString);
    }


    // Try to process a text bind that mirrors a float bind. Returns true if handled.
    private bool TryHandleNumericTextPair(string elementId)
    {
        bool HandledText(NuiBind<float> numeric, NuiBind<string> text)
        {
            if (!elementId.Equals(text.Key, StringComparison.OrdinalIgnoreCase)) return false;
            string? s = Token().GetBindValue(text) ?? string.Empty;
            string sanitized = SanitizeNumericString(s);
            Token().SetBindValue(text, sanitized);
            if (float.TryParse(sanitized, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
            {
                Token().SetBindValue(numeric, f);
            }
            return true;
        }

        return
            HandledText(View.TransformX, View.TransformXString) ||
            HandledText(View.TransformY, View.TransformYString) ||
            HandledText(View.TransformZ, View.TransformZString) ||
            HandledText(View.RotationX, View.RotationXString) ||
            HandledText(View.RotationY, View.RotationYString) ||
            HandledText(View.RotationZ, View.RotationZString) ||
            HandledText(View.Scale, View.ScaleString) ||
            HandledText(View.PositionX, View.PositionXString) ||
            HandledText(View.PositionY, View.PositionYString) ||
            HandledText(View.PositionZ, View.PositionZString) ||
            HandledText(View.PositionStep, View.PositionStepString);
    }

    // Utility: execute an action while ignoring specified elementIds in the watch loop.
    private void WithWatchBlacklist(IEnumerable<string> elementIds, Action action)
    {
        foreach (string id in elementIds)
        {
            if (!string.IsNullOrEmpty(id)) _watchBlacklist.Add(id);
        }

        try
        {
            action();
        }
        finally
        {
            foreach (string id in elementIds)
            {
                if (!string.IsNullOrEmpty(id)) _watchBlacklist.Remove(id);
            }
        }
    }

    // Helper to decide if a watch event should be ignored now.
    private bool IsBlacklisted(string? elementId)
        => !string.IsNullOrEmpty(elementId) && _watchBlacklist.Contains(elementId!);

    // Common slider ids (so we can treat them uniformly).
    private static bool IsSliderId(string? id)
        => !string.IsNullOrEmpty(id) && id.EndsWith("_slider", StringComparison.OrdinalIgnoreCase);

    private void ApplyPlcSafe()
    {
        UpdatePlc();
    }

    private static string SanitizeNumericString(string input)
    {
        // Allow optional leading '-' and a single '.'; remove everything else.
        bool seenDot = false;
        bool seenSign = false;
        List<char> chars = new List<char>(input.Length);

        foreach (char c in input)
        {
            if (!seenSign && chars.Count == 0 && (c == '-' || c == '+'))
            {
                chars.Add(c);
                seenSign = true;
                continue;
            }

            if (char.IsDigit(c))
            {
                chars.Add(c);
                continue;
            }

            if (c == '.' && !seenDot)
            {
                chars.Add('.');
                seenDot = true;
            }
        }

        if (chars.Count == 0 || (chars.Count == 1 && (chars[0] == '-' || chars[0] == '+')))
            return string.Empty;

        if (chars[0] == '+')
            chars.RemoveAt(0);

        return new string(chars.ToArray());
    }

    private void SanitizeInputs()
    {
        SanitizePositions();
        SanitizeTransforms();
        SanitizeScale();
        SanitizeRotations();

        string? appearance = Token().GetBindValue(View.AppearanceValueString);
        if (appearance is null) return;
        string sanitized = SanitizeNumericString(appearance);
        Token().SetBindValue(View.AppearanceValueString, sanitized);

        if (int.TryParse(sanitized, out int value))
        {
            value = Math.Abs(value);
            Token().SetBindValue(View.AppearanceValue, value);
        }
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
        Token().SetBindWatch(View.AppearanceValueString, b);

        // Numeric binds watched; we avoid loops using blacklist + scoped toggling.
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

        Token().SetBindWatch(View.PositionStep, b);
        Token().SetBindWatch(View.PositionStepString, b);
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

    // Pull values from the selected PLC into the binds, similar to the reference controller's Update().
    private void UpdateFromModel()
    {
        ToggleBindWatch(false);

        bool selectionAvailable = _model.Selected != null;
        Token().SetBindValue(View.ValidObjectSelected, selectionAvailable);

        if (_model.Selected == null)
        {
            ToggleBindWatch(true);
            return;
        }

        BindNewValuesFromSelected();

        ToggleBindWatch(true);
    }

    public override void Close()
    {
    }
}
