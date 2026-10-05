using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Conditions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using Newtonsoft.Json;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Nui;

/// <summary>
/// Presenter for the in-game NPC conversation window.
/// Manages NPC text display, choice navigation, text pagination, and dialogue advancement.
/// Implements IAutoCloseOnMove to end conversation when the player walks away.
/// </summary>
public sealed class ConversationPresenter : ScryPresenter<ConversationView>, IAutoCloseOnMove
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly NwPlayer _player;
    private readonly AmiaDialogueService _amiaDialogueService;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private readonly NuiImageInput _imageInput = new();
    private ImageContext? _pressedContext;

    private float _scaleFactor = 1f;

    // Cached visible choices for the current node
    private List<DialogueChoice> _visibleChoices = [];
    private DialogueNodeId? _displayedNodeId;
    private DialogueSession? _displayedSession;
    private bool _isRefreshingChoices;
    private bool _isAdvancing;
    private bool _isClosed;
    private int _refreshVersion;
    private int _choicePage; // For paginating choices when >5

    [Inject] private Lazy<DialogueConditionRegistry>? ConditionRegistry { get; init; }
    [Inject] private DevicePropertyService DevicePropertyService { get; init; } = null!;

    public ConversationPresenter(ConversationView view, NwPlayer player, AmiaDialogueService amiaDialogueService)
    {
        View = view;
        _player = player;
        _amiaDialogueService = amiaDialogueService;
    }

    public override ConversationView View { get; }
    public override NuiWindowToken Token() => _token;

    // IAutoCloseOnMove: close if player moves 5m away
    TimeSpan IAutoCloseOnMove.AutoClosePollInterval => TimeSpan.FromSeconds(1);
    float IAutoCloseOnMove.AutoCloseMovementThreshold => 5.0f;

    public override void InitBefore()
    {
        // Calculate GUI-scale factor and pass it to the view before layout is built
        int guiScalePercent = DevicePropertyService.GetGuiScale(_player);
        _scaleFactor = guiScalePercent / 100f;
        if (_scaleFactor <= 0f) _scaleFactor = 1f;

        View.SetScaleFactor(_scaleFactor);

        _window = new NuiWindow(View.RootLayout(), null!)
        {
            Geometry = new NuiRect(
                ConversationView.BaseWindowX / _scaleFactor,
                ConversationView.BaseWindowY / _scaleFactor,
                ConversationView.BaseWindowW / _scaleFactor,
                ConversationView.BaseWindowH / _scaleFactor),
            Transparent = true,
            Border = false,
            Resizable = false,
            Closable = false,
            Collapsed = false
        };
    }

    public override void Create()
    {
        if (_window == null)
        {
            throw new InvalidOperationException("Conversation window not configured");
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
        {
            throw new InvalidOperationException("Unable to open conversation window");
        }

        // Set initial static binds
        _token.SetBindValue(View.GoodbyeText, "Goodbye");
        _token.SetBindValue(View.ShowMoreButton, false);
        for (int i = 0; i < ConversationView.MaxVisibleChoices; i++)
            _token.SetBindValue(View.ChoiceVisible[i], false);

        // Refresh the view with current session state
        RefreshView();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (_isClosed) return;
        if (eventData.EventType == NuiEventType.Close)
        {
            _amiaDialogueService.EndDialogue(_player, "window_closed");
            return;
        }
        if (eventData.EventType is not (NuiEventType.MouseDown or NuiEventType.MouseUp)) return;

        bool image = View.Graphical.ImageActionIds.Contains(eventData.ElementId);
        int? button = null;
        if (image)
        {
            try { button = eventData.GetEventPayload<NuiMousePayload>()?.MouseButton; }
            catch (JsonException) { /* Invalid payloads must not become left-button actions. */ }
        }

        if (HandleImageEvent(eventData.EventType, eventData.ElementId, button, image,
                image && IsImageActionEnabled(eventData.ElementId), GetImageContext()))
            _ = HandleActionAsync(eventData.ElementId);
    }

    internal bool HandleImageEvent(NuiEventType eventType, string elementId, int? button, bool image,
        bool enabled, ImageContext? context)
    {
        if (eventType is not (NuiEventType.MouseDown or NuiEventType.MouseUp)) return false;
        if (eventType == NuiEventType.MouseDown && image)
            _pressedContext = enabled && button == 0 ? context : null;
        if (eventType == NuiEventType.MouseUp)
            enabled &= _pressedContext != null && _pressedContext == context;

        bool activate = _imageInput.Handle(eventType, elementId, button, enabled, image);
        if (eventType == NuiEventType.MouseUp) _pressedContext = null;
        return activate;
    }

    internal readonly record struct ImageContext(DialogueSession Session, DialogueNodeId NodeId,
        int TextPage, int ChoicePage, int RefreshVersion);

    private ImageContext? GetImageContext() => _amiaDialogueService.GetActiveSession(_player) is { IsEnded: false } session
        ? new(session, session.CurrentNodeId, session.TextPage, _choicePage, _refreshVersion) : null;

    private void ResetInput()
    {
        _imageInput.Reset();
        _pressedContext = null;
    }

    private bool ChoicesReady(DialogueSession session) => !_isRefreshingChoices &&
        ReferenceEquals(_displayedSession, session) && _displayedNodeId == session.CurrentNodeId;

    private bool IsImageActionEnabled(string id)
    {
        if (_isClosed || _amiaDialogueService.GetActiveSession(_player) is not { IsEnded: false } session) return false;
        return CanActivateImageAction(id, _isAdvancing || session.Playback.IsBusy, ChoicesReady(session),
            _choicePage, _visibleChoices.Count, session.HasPreviousTextPage(), session.HasNextTextPage());
    }

    internal static bool CanActivateImageAction(string id, bool busy, bool choicesReady, int choicePage,
        int choiceCount, bool hasPrevious, bool hasNext)
    {
        if (busy) return false;
        return id switch
        {
            "conv_close" or "btn_goodbye" => true,
            "btn_prev_text" => hasPrevious,
            "btn_next_text" => hasNext,
            "btn_more" => choicesReady && choiceCount > ConversationView.MaxVisibleChoices,
            _ => choicesReady && GetChoiceIndex(id, choicePage, choiceCount) != null
        };
    }

    internal static int? GetChoiceIndex(string id, int page, int count)
    {
        const string prefix = "btn_choice_";
        if (!id.StartsWith(prefix, StringComparison.Ordinal) ||
            !int.TryParse(id[prefix.Length..], out int slot) || slot < 0 || slot >= ConversationView.MaxVisibleChoices ||
            page < 0 || count <= 0 || page > (count - 1) / ConversationView.MaxVisibleChoices) return null;
        int index = page * ConversationView.MaxVisibleChoices + slot;
        return index < count ? index : null;
    }

    public override void UpdateView()
    {
        RefreshView();
    }

    public override void Close()
    {
        if (_isClosed) return;
        _isClosed = true;
        _refreshVersion++;
        ResetInput();
        _amiaDialogueService.EndDialogue(_player, "window_closed", closeWindow: false);
        try { _token.Close(); }
        catch { /* ignore if already closed */ }
    }

    // ──────────────────── Image Action Routing ────────────────────

    private async Task HandleActionAsync(string elementId)
    {
        if (!IsImageActionEnabled(elementId)) return;
        ResetInput();
        try
        {
            switch (elementId)
            {
                case "conv_close":
                    _amiaDialogueService.EndDialogue(_player, "window_closed");
                    return;

                case "btn_goodbye":
                    _amiaDialogueService.EndDialogue(_player, "goodbye");
                    return;

                case "btn_prev_text":
                {
                    DialogueSession? s = _amiaDialogueService.GetActiveSession(_player);
                    if (s != null && s.HasPreviousTextPage())
                    {
                        s.TextPage--;
                        RefreshTextPanel();
                    }

                    return;
                }

                case "btn_next_text":
                {
                    DialogueSession? s = _amiaDialogueService.GetActiveSession(_player);
                    if (s != null && s.HasNextTextPage())
                    {
                        s.TextPage++;
                        RefreshTextPanel();
                    }

                    return;
                }

                case "btn_more":
                    int pageCount = Math.Max(1, (_visibleChoices.Count + ConversationView.MaxVisibleChoices - 1) / ConversationView.MaxVisibleChoices);
                    _choicePage = (_choicePage + 1) % pageCount;
                    await RefreshChoicesAsync();
                    return;
            }

            // Choice buttons: btn_choice_0..4
            if (GetChoiceIndex(elementId, _choicePage, _visibleChoices.Count) is { } absoluteIndex)
            {
                if (_isAdvancing || _displayedNodeId is not { } nodeId) return;
                DialogueSession? advancingSession = _displayedSession;
                Guid choiceId = _visibleChoices[absoluteIndex].Id;
                _isAdvancing = true;
                _refreshVersion++;
                ResetInput();
                ApplyControlState();
                try
                {
                    await _amiaDialogueService.AdvanceDialogueAsync(_player, nodeId, choiceId);
                }
                finally
                {
                    await NwTask.SwitchToMainThread();
                    _isAdvancing = false;
                }
                bool success = !_isClosed && ReferenceEquals(_amiaDialogueService.GetActiveSession(_player), advancingSession)
                    && advancingSession is { IsEnded: false };
                if (success)
                {
                    _choicePage = 0;

                    // If the dialogue ended, AdvanceDialogueAsync already called
                    // EndDialogue → WindowDirector.CloseWindow → Close(). No need
                    // to close again here. Just refresh if still active.
                    DialogueSession? session = _amiaDialogueService.GetActiveSession(_player);
                    if (session != null && !session.IsEnded)
                    {
                        await NwTask.SwitchToMainThread();
                        await RefreshViewAsync();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await NwTask.SwitchToMainThread();
            Log.Error(ex, "Failed to handle dialogue image action '{ElementId}'", elementId);
            if (!_isClosed) _amiaDialogueService.EndDialogue(_player, "window_failed");
        }
    }

    // ──────────────────── View Refresh ────────────────────

    private async void RefreshView()
    {
        try { await RefreshViewAsync(); }
        catch (Exception ex)
        {
            await NwTask.SwitchToMainThread();
            Log.Error(ex, "Failed to refresh conversation window");
            if (!_isClosed) _amiaDialogueService.EndDialogue(_player, "window_failed");
        }
    }

    private async Task RefreshViewAsync()
    {
        if (_isClosed || _isAdvancing) return;
        ResetInput();
        DialogueSession? session = _amiaDialogueService.GetActiveSession(_player);
        if (session == null)
        {
            Close();
            return;
        }

        RefreshPortrait(session);
        RefreshTextPanel();
        await RefreshChoicesAsync();
    }

    private void RefreshPortrait(DialogueSession session)
    {
        _token.SetBindValue(View.SpeakerName, session.GetNpcName());
        string portraitResRef = session.GetPortraitResRef();
        _token.SetBindValue(View.NpcPortrait, string.IsNullOrEmpty(portraitResRef) ? "" : portraitResRef + "h");
    }

    private void RefreshTextPanel()
    {
        if (_isClosed) return;
        ResetInput();
        DialogueSession? session = _amiaDialogueService.GetActiveSession(_player);
        if (session == null) return;

        string text = session.GetCurrentTextPage();
        int totalPages = session.GetTotalTextPages();

        _token.SetBindValue(View.NpcText, text);

        bool multiPage = totalPages > 1;
        _token.SetBindValue(View.ShowTextPagination, multiPage);

        if (multiPage)
        {
            _token.SetBindValue(View.TextPageInfo, $"{session.TextPage + 1}/{totalPages}");
        }
        else
        {
            _token.SetBindValue(View.TextPageInfo, "");
        }

        ApplyControlState();
    }

    private void ApplyControlState()
    {
        if (_isClosed) return;
        bool controls = IsImageActionEnabled("btn_goodbye");
        DialogueSession? session = _amiaDialogueService.GetActiveSession(_player);
        _token.SetBindValue(View.Graphical.ControlsEnabled, controls);
        _token.SetBindValue(View.Graphical.ChoicesEnabled, controls && session != null && ChoicesReady(session));
        bool previous = IsImageActionEnabled("btn_prev_text");
        bool next = IsImageActionEnabled("btn_next_text");
        _token.SetBindValue(View.ShowPrevTextPage, previous);
        _token.SetBindValue(View.ShowNextTextPage, next);
        _token.SetBindValue(View.Graphical.PreviousColor, previous ? ConversationGraphicalView.Gold : ConversationGraphicalView.Muted);
        _token.SetBindValue(View.Graphical.NextColor, next ? ConversationGraphicalView.Gold : ConversationGraphicalView.Muted);
    }

    private async Task RefreshChoicesAsync()
    {
        if (_isClosed || _isAdvancing) return;
        DialogueSession? session = _amiaDialogueService.GetActiveSession(_player);
        if (session == null) return;

        ResetInput();
        _isRefreshingChoices = true;
        ApplyControlState();

        if (ConditionRegistry?.Value == null)
        {
            Log.Warn("DialogueConditionRegistry not available for choice evaluation");
            return;
        }

        // Get all visible choices
        int refreshVersion = ++_refreshVersion;
        DialogueNodeId nodeId = session.CurrentNodeId;
        List<DialogueChoice> choices = await session.GetVisibleChoicesAsync(ConditionRegistry.Value);
        await NwTask.SwitchToMainThread();
        if (_isClosed || refreshVersion != _refreshVersion || session.IsEnded ||
            !ReferenceEquals(_amiaDialogueService.GetActiveSession(_player), session) || session.CurrentNodeId != nodeId) return;
        ResetInput();
        _isRefreshingChoices = false;
        _visibleChoices = choices;
        _displayedNodeId = nodeId;
        _displayedSession = session;

        // Calculate pagination
        int totalPages = Math.Max(1,
            (int)Math.Ceiling(_visibleChoices.Count / (double)ConversationView.MaxVisibleChoices));

        // Clamp choice page
        if (_choicePage >= totalPages) _choicePage = Math.Max(0, totalPages - 1);

        int startIndex = _choicePage * ConversationView.MaxVisibleChoices;

        // Update choice slots
        for (int i = 0; i < ConversationView.MaxVisibleChoices; i++)
        {
            int choiceIndex = startIndex + i;
            if (choiceIndex < _visibleChoices.Count)
            {
                _token.SetBindValue(View.ChoiceTexts[i], _visibleChoices[choiceIndex].IsContinue ? "Continue" : _visibleChoices[choiceIndex].ResponseText);
                _token.SetBindValue(View.ChoiceVisible[i], true);
            }
            else
            {
                _token.SetBindValue(View.ChoiceTexts[i], "");
                _token.SetBindValue(View.ChoiceVisible[i], false);
            }
        }

        // Show "More" button if choices overflow
        bool hasMore = _visibleChoices.Count > ConversationView.MaxVisibleChoices;
        _token.SetBindValue(View.ShowMoreButton, hasMore);
        _token.SetBindValue(View.MoreButtonText, hasMore ? $"More ({_choicePage + 1}/{totalPages})" : "More");
        ApplyControlState();
    }
}
