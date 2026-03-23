using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Conditions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

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
    private readonly DialogueService _dialogueService;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    private float _scaleFactor = 1f;

    // Cached visible choices for the current node
    private List<DialogueChoice> _visibleChoices = [];
    private int _choicePage; // For paginating choices when >5

    [Inject] private Lazy<DialogueConditionRegistry>? ConditionRegistry { get; init; }
    [Inject] private DevicePropertyService DevicePropertyService { get; init; } = null!;

    public ConversationPresenter(ConversationView view, NwPlayer player, DialogueService dialogueService)
    {
        View = view;
        _player = player;
        _dialogueService = dialogueService;
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

        DialogueSession? session = _dialogueService.GetActiveSession(_player);
        string title = session != null ? session.GetNpcName() : "Conversation";

        _window = new NuiWindow(View.RootLayout(), title)
        {
            Geometry = new NuiRect(
                ConversationView.BaseWindowX / _scaleFactor,
                ConversationView.BaseWindowY / _scaleFactor,
                ConversationView.BaseWindowW / _scaleFactor,
                ConversationView.BaseWindowH / _scaleFactor),
            Resizable = true,
            Closable = true
        };
    }

    public override void Create()
    {
        if (_window == null)
        {
            _player.SendServerMessage("Conversation window not configured.", ColorConstants.Orange);
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
        {
            _player.SendServerMessage("Unable to open conversation.", ColorConstants.Orange);
            return;
        }

        // Set initial static binds
        _token.SetBindValue(View.GoodbyeText, "Goodbye");

        // Refresh the view with current session state
        RefreshView();
    }

    public override async void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                await HandleClick(eventData.ElementId);
                break;
            case NuiEventType.Close:
                _dialogueService.EndDialogue(_player, "window_closed");
                break;
        }
    }

    public override void UpdateView()
    {
        RefreshView();
    }

    public override void Close()
    {
        try { _token.Close(); }
        catch { /* ignore if already closed */ }
    }

    // ──────────────────── Click Routing ────────────────────

    private async Task HandleClick(string elementId)
    {
        switch (elementId)
        {
            case "btn_goodbye":
                _dialogueService.EndDialogue(_player, "goodbye");
                return;

            case "btn_prev_text":
            {
                DialogueSession? s = _dialogueService.GetActiveSession(_player);
                if (s != null && s.HasPreviousTextPage())
                {
                    s.TextPage--;
                    RefreshTextPanel();
                }

                return;
            }

            case "btn_next_text":
            {
                DialogueSession? s = _dialogueService.GetActiveSession(_player);
                if (s != null && s.HasNextTextPage())
                {
                    s.TextPage++;
                    RefreshTextPanel();
                }

                return;
            }

            case "btn_more":
                _choicePage++;
                await RefreshChoicesAsync();
                return;
        }

        // Choice buttons: btn_choice_0..4
        if (elementId.StartsWith("btn_choice_"))
        {
            string indexStr = elementId["btn_choice_".Length..];

            if (int.TryParse(indexStr, out int slotIndex))
            {
                int absoluteIndex = (_choicePage * ConversationView.MaxVisibleChoices) + slotIndex;

                if (absoluteIndex >= 0 && absoluteIndex < _visibleChoices.Count)
                {
                    bool success = await _dialogueService.AdvanceDialogueAsync(_player, absoluteIndex);
                    if (success)
                    {
                        _choicePage = 0;

                        // If the dialogue ended, AdvanceDialogueAsync already called
                        // EndDialogue → WindowDirector.CloseWindow → Close(). No need
                        // to close again here. Just refresh if still active.
                        DialogueSession? session = _dialogueService.GetActiveSession(_player);
                        if (session != null && !session.IsEnded)
                        {
                            await NwTask.SwitchToMainThread();
                            RefreshView();
                        }
                    }
                }
            }
        }
    }

    // ──────────────────── View Refresh ────────────────────

    private async void RefreshView()
    {
        DialogueSession? session = _dialogueService.GetActiveSession(_player);
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
        string portraitResRef = session.GetPortraitResRef();
        // NWN portraits: the resref is stored without size suffix; large portrait = resref + "l"
        // For NuiImage, use the portrait resref directly
        _token.SetBindValue(View.NpcPortrait, portraitResRef + "l");
    }

    private void RefreshTextPanel()
    {
        DialogueSession? session = _dialogueService.GetActiveSession(_player);
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

        _token.SetBindValue(View.ShowPrevTextPage, session.HasPreviousTextPage());
        _token.SetBindValue(View.ShowNextTextPage, session.HasNextTextPage());
    }

    private async Task RefreshChoicesAsync()
    {
        DialogueSession? session = _dialogueService.GetActiveSession(_player);
        if (session == null) return;

        if (ConditionRegistry?.Value == null)
        {
            Log.Warn("DialogueConditionRegistry not available for choice evaluation");
            return;
        }

        // Get all visible choices
        _visibleChoices = await session.GetVisibleChoicesAsync(ConditionRegistry.Value);
        await NwTask.SwitchToMainThread();

        // Calculate pagination
        int startIndex = _choicePage * ConversationView.MaxVisibleChoices;
        int totalPages = Math.Max(1,
            (int)Math.Ceiling(_visibleChoices.Count / (double)ConversationView.MaxVisibleChoices));

        // Clamp choice page
        if (_choicePage >= totalPages) _choicePage = Math.Max(0, totalPages - 1);

        // Update choice slots
        for (int i = 0; i < ConversationView.MaxVisibleChoices; i++)
        {
            int choiceIndex = startIndex + i;
            if (choiceIndex < _visibleChoices.Count)
            {
                _token.SetBindValue(View.ChoiceTexts[i], _visibleChoices[choiceIndex].ResponseText);
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
    }
}
