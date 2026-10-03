using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Notes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using Newtonsoft.Json;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Player;

/// <summary>
/// Presenter for the Player Codex view.
/// Manages tab switching, category filtering, paginated entry list, and detail pane.
/// </summary>
public sealed class PlayerCodexPresenter : ScryPresenter<PlayerCodexView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private readonly CodexImageInput _imageInput = new();

    // Injected services
    [Inject] private Lazy<CodexQueryService>? QueryService { get; init; }
    [Inject] private Lazy<ICommandDispatcher>? Commands { get; init; }
    [Inject] private Lazy<CodexNoteDraftStore>? DraftStore { get; init; }
    [Inject] private Lazy<IIndustryMembershipService>? MembershipService { get; init; }
    [Inject] private Lazy<IIndustryRepository>? IndustryRepository { get; init; }
    [Inject] private Lazy<TraitSelectionWindowService>? SelectionWindowService { get; init; }

    // State
    private CodexTab _activeTab = CodexTab.Knowledge;
    private string _activeCategory = "all";
    private int _currentPage;
    private Guid? _selectedNoteId;
    private CodexNoteDraft? _draft;
    private string? _pendingAction;
    private bool _busy;
    private bool _closed;
    private int _loadVersion;
    private string _searchTerm = "";
    private int PageSize => _activeTab == CodexTab.Notes ? PlayerCodexView.NotesPerPage : PlayerCodexView.EntriesPerPage;
    private List<ICodexDisplayItem> _currentEntries = new();
    private CharacterId? _characterId;
    private List<IndustryMembership> _memberships = new();
    private IndustryMembership? _activeMembership;

    public PlayerCodexPresenter(PlayerCodexView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override PlayerCodexView View { get; }
    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), null!)
        {
            Geometry = View.Geometry, Transparent = true, Border = false,
            Resizable = false, Closable = false, Collapsed = false
        };

        // Resolve CharacterId from the player's ds_pckey item
        _characterId = ResolveCharacterId();
    }

    public override void Create()
    {
        if (_window == null)
        {
            _player.SendServerMessage("Codex window not configured.", ColorConstants.Orange);
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
        {
            _player.SendServerMessage("Unable to open the codex right now.", ColorConstants.Orange);
            return;
        }

        Position(-1, -1);
        _token.SetBindValue(View.CanInteract, true);
        _token.SetBindValue(View.ControlColor, PlayerCodexView.Gold);
        RefreshTabTextures();
        _token.SetBindValue(View.CanCloseWindow, true);
        _token.SetBindValue(View.ShowConfirmation, false);
        _token.SetBindValue(View.ShowNoteActions, false);
        _token.SetBindValue(View.Status, "");
        _token.SetBindValue(View.NoteSearch, "");

        if (_characterId == null)
        {
            _player.SendServerMessage("No character key found. Cannot open codex.", ColorConstants.Orange);
            SetDetailContent("Error", "No character key found on your character.");
            return;
        }

        // Restore an editor draft if a previous window was forcibly closed.
        _ = OpenInitialTabAsync();
    }

    private async Task OpenInitialTabAsync()
    {
        CodexNoteDraft? draft = _characterId is { } id ? DraftStore?.Value.Get(_player, id) : null;
        await HandleClickAsync(draft == null ? "tab_knowledge" : "tab_notes");
        if (!_closed && draft != null) BeginEdit(null, draft);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (_closed) return;
        bool image = View.ImageActionIds.Contains(eventData.ElementId);
        if (eventData.EventType is NuiEventType.MouseDown or NuiEventType.MouseUp)
        {
            int? button = null;
            if (image)
            {
                try { button = eventData.GetEventPayload<CodexMousePayload>()?.MouseButton; }
                catch (JsonException) { /* Missing or invalid buttons must not become left clicks. */ }
            }
            if (_imageInput.Handle(eventData.EventType, eventData.ElementId, button,
                    image && IsImageActionEnabled(eventData.ElementId)))
                _ = HandleClickAsync(eventData.ElementId);
            return;
        }

        // Image actions never dispatch Click as well as MouseUp. Native note buttons still use Click.
        if (eventData.EventType == NuiEventType.Click && !image &&
            eventData.ElementId is "note_new" or "note_edit" or "note_delete" or "note_save" or
                "note_cancel" or "note_search" or "note_clear_search")
            _ = HandleClickAsync(eventData.ElementId);
        else if (eventData.EventType == NuiEventType.Watch && _draft != null && !_busy &&
                 (eventData.ElementId == View.NoteTitle.Key || eventData.ElementId == View.NoteContent.Key ||
                  eventData.ElementId == View.NoteCategorySelection.Key))
            ReadDraft();
    }

    private bool IsImageActionEnabled(string id)
    {
        if (_busy || _closed) return false;
        if (id is "codex_confirm" or "codex_keep") return _pendingAction != null;
        if (id == "btn_select_traits") return _activeTab == CodexTab.Traits;
        if (id == "btn_prev_page") return _currentPage > 0;
        if (id == "btn_next_page") return (_currentPage + 1) * PageSize < _currentEntries.Count;
        if (id.StartsWith("btn_entry_", StringComparison.Ordinal))
            return int.TryParse(id["btn_entry_".Length..], out int row) && row >= 0 && row < PageSize &&
                   _currentPage * PageSize + row < _currentEntries.Count;
        return true;
    }

    private void Position(float x, float y) => _token.SetBindValue(View.Geometry,
        new NuiRect(x, y, PlayerCodexView.WindowW, PlayerCodexView.WindowH));

    private void RefreshTabTextures()
    {
        foreach ((CodexTab tab, NuiBind<string> texture) in View.TabTextures)
            _token.SetBindValue(texture, tab == _activeTab ? "ui_cdx_tab_s_v2" : "ui_cdx_tab_n_v2");
    }

    public override void UpdateView() { }

    public override void Close()
    {
        if (_closed) return;
        _closed = true;
        _imageInput.Reset();
        _loadVersion++;
        try { _token.Close(); }
        catch { /* token may already be closed by the client */ }
    }

    private async Task HandleClickAsync(string elementId)
    {
        if (_closed || _busy) return;
        try
        {
            if (View.ImageActionIds.Contains(elementId) && !IsImageActionEnabled(elementId)) return;
            if (elementId is "codex_center" or "codex_top_left")
            {
                Position(elementId == "codex_center" ? -1 : 24, elementId == "codex_center" ? -1 : 24);
                return;
            }
            if (elementId == "codex_keep")
            {
                ClearConfirmation();
                return;
            }
            if (elementId == "codex_confirm")
            {
                string? action = _pendingAction;
                ClearConfirmation();
                if (action == "note_confirm_delete")
                    await DeleteNoteAsync();
                else if (action != null)
                {
                    EndEdit();
                    await HandleClickAsync(action);
                }
                return;
            }

            bool navigating = elementId.StartsWith("tab_") || elementId.StartsWith("cat_") ||
                              elementId.StartsWith("btn_entry_") || elementId is "btn_prev_page" or
                              "btn_next_page" or "codex_close" or "note_new" or "note_cancel" or
                              "note_search" or "note_clear_search";
            if (_draft != null && navigating)
            {
                ReadDraft();
                if (_draft.IsDirty)
                {
                    Confirm(elementId, "Discard", "Discard unsaved changes?");
                    return;
                }
                EndEdit();
            }
            ClearConfirmation();

            switch (elementId)
            {
                case "tab_knowledge": await SwitchTabAsync(CodexTab.Knowledge); break;
                case "tab_quests": await SwitchTabAsync(CodexTab.Quests); break;
                case "tab_notes": await SwitchTabAsync(CodexTab.Notes); break;
                case "tab_reputation": await SwitchTabAsync(CodexTab.Reputation); break;
                case "tab_traits": await SwitchTabAsync(CodexTab.Traits); break;
                case "tab_economy": await SwitchTabAsync(CodexTab.Economy); break;
                case "btn_select_traits": SelectionWindowService?.Value.Open(_player); break;
                case "btn_prev_page":
                    if (_currentPage > 0) { _currentPage--; RefreshEntryList(); }
                    break;
                case "btn_next_page":
                    int maxPage = Math.Max(0, (_currentEntries.Count - 1) / PageSize);
                    if (_currentPage < maxPage) { _currentPage++; RefreshEntryList(); }
                    break;
                case "codex_close": RaiseCloseEvent(); Close(); break;
                case "note_new": BeginEdit(null); break;
                case "note_edit":
                    if (SelectedNote() is { CanPlayerEdit: true } note) BeginEdit(note);
                    break;
                case "note_cancel": ShowSelectedNote(); break;
                case "note_save": await SaveNoteAsync(); break;
                case "note_delete":
                    if (SelectedNote() is { CanPlayerEdit: true })
                        Confirm("note_confirm_delete", "Delete", "Permanently delete this note?");
                    break;
                case "note_search":
                case "note_clear_search":
                    if (_activeTab != CodexTab.Notes) break;
                    _searchTerm = elementId == "note_clear_search" ? "" : (_token.GetBindValue(View.NoteSearch) ?? "").Trim();
                    _token.SetBindValue(View.NoteSearch, _searchTerm);
                    _currentPage = 0;
                    _selectedNoteId = null;
                    await ReloadEntriesAsync();
                    break;
                default:
                    if (elementId.StartsWith("cat_"))
                        await ApplyCategoryAsync(elementId[4..]);
                    else if (elementId.StartsWith("btn_entry_") && int.TryParse(elementId["btn_entry_".Length..], out int row))
                        SelectEntry(row);
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Codex action failed for {CharacterId}", _characterId);
            await NwTask.SwitchToMainThread();
            if (!_closed) SetStatus("Unable to complete the action. Please try again.");
        }
    }

    private async Task SwitchTabAsync(CodexTab tab)
    {
        _activeTab = tab;
        RefreshTabTextures();
        _activeCategory = "all";
        _currentPage = 0;
        _selectedNoteId = null;
        SwapCategorySidebar();
        SwapEntryListPane();
        _token.SetBindValue(View.ShowNoteActions, false);
        SetSelectTraitsVisible(tab == CodexTab.Traits);
        if (tab == CodexTab.Notes) _token.SetBindValue(View.NoteSearch, _searchTerm);
        await ReloadEntriesAsync();
    }

    private async Task ApplyCategoryAsync(string category)
    {
        _activeCategory = category;
        SwapCategorySidebar();
        _currentPage = 0;
        _selectedNoteId = null;
        await ReloadEntriesAsync();
    }

    private async Task ReloadEntriesAsync(Guid? selectNoteId = null)
    {
        int version = ++_loadVersion;
        _currentEntries = new();
        RefreshEntryList();
        _token.SetBindValue(View.ShowNoteActions, false);
        SetStatus("Loading...");
        SetDetailContent("Loading", "Loading entries...");
        List<ICodexDisplayItem> entries;
        try
        {
            entries = await LoadEntries();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load Codex entries for {CharacterId}", _characterId);
            await NwTask.SwitchToMainThread();
            if (!_closed && version == _loadVersion)
            {
                SetStatus("Unable to load entries. Please try again.");
                if (_draft == null) SetDetailContent("Unable to Load", "Select the tab again to retry.");
            }
            return;
        }
        await NwTask.SwitchToMainThread();
        if (_closed || version != _loadVersion) return;
        _currentEntries = entries;
        if (selectNoteId.HasValue)
        {
            int index = entries.FindIndex(e => e is NoteDisplayItem n && n.Note.Id == selectNoteId);
            if (index >= 0)
            {
                _selectedNoteId = selectNoteId;
                _currentPage = index / PageSize;
            }
        }
        RefreshEntryList();
        if (_activeTab == CodexTab.Economy) RefreshProficiencyDisplay();
        if (_draft != null) return;
        SetStatus("");
        if (_activeTab == CodexTab.Notes) ShowSelectedNote();
        else SetDetailContent("Select an Entry", "Choose an entry from the list to view its details.");
    }

    private CodexNoteEntry? SelectedNote() => _activeTab == CodexTab.Notes
        ? _currentEntries.OfType<NoteDisplayItem>().FirstOrDefault(n => n.Note.Id == _selectedNoteId)?.Note
        : null;

    private void BeginEdit(CodexNoteEntry? note, CodexNoteDraft? restoredDraft = null)
    {
        if (_activeTab != CodexTab.Notes || _characterId == null) return;
        NoteCategory category = Enum.TryParse(_activeCategory, true, out NoteCategory cat) && cat.IsPlayerCategory()
            ? cat : NoteCategory.General;
        _draft = restoredDraft ?? new CodexNoteDraft(note, category);
        DraftStore?.Value.Set(_player, _characterId.Value, _draft);
        _token.SetGroupLayout(View.DetailGroup, View.BuildNoteEditor());
        _token.SetBindValue(View.NoteTitle, _draft.Title);
        _token.SetBindValue(View.NoteContent, _draft.Content);
        _token.SetBindValue(View.NoteCategorySelection, (int)_draft.Category);
        _token.SetBindWatch(View.NoteTitle, true);
        _token.SetBindWatch(View.NoteContent, true);
        _token.SetBindWatch(View.NoteCategorySelection, true);
        _token.SetBindValue(View.CanCloseWindow, false);
        SetStatus("Save your note or cancel editing.");
    }

    private void ReadDraft()
    {
        if (_draft == null) return;
        _draft.Title = _token.GetBindValue(View.NoteTitle) ?? "";
        _draft.Content = _token.GetBindValue(View.NoteContent) ?? "";
        _draft.Category = (NoteCategory)_token.GetBindValue(View.NoteCategorySelection);
        if (_characterId is { } id) DraftStore?.Value.Set(_player, id, _draft);
    }

    private void EndEdit()
    {
        if (_draft == null) return;
        _draft = null;
        if (_characterId is { } id) DraftStore?.Value.Remove(_player, id);
        _token.SetBindWatch(View.NoteTitle, false);
        _token.SetBindWatch(View.NoteContent, false);
        _token.SetBindWatch(View.NoteCategorySelection, false);
        _token.SetGroupLayout(View.DetailGroup, View.BuildDetailContent());
        _token.SetBindValue(View.CanCloseWindow, !_busy);
        ShowSelectedNote();
    }

    private async Task SaveNoteAsync()
    {
        if (_draft == null || _characterId == null || Commands?.Value == null) return;
        if (_draft.IsSaving)
        {
            SetStatus("This note is already being saved. Please wait.");
            return;
        }
        ReadDraft();
        CodexNoteDraft draft = _draft;
        string title = draft.Title;
        string content = draft.Content;
        NoteCategory category = draft.Category;
        try
        {
            CodexNoteEntry.ValidatePlayerInput(draft.Title, draft.Content, draft.Category);
        }
        catch (ArgumentException ex)
        {
            SetStatus(ex.Message.Split(" (Parameter")[0]);
            return;
        }
        SetBusy(true);
        draft.IsSaving = true;
        SetStatus("Saving...");
        try
        {
            CommandResult result = draft.NoteId is { } id
                ? await Commands.Value.DispatchAsync(new EditNoteCommand
                {
                    CharacterId = _characterId.Value, NoteId = id,
                    Title = title, NewContent = content, Category = category
                })
                : await Commands.Value.DispatchAsync(new AddNoteCommand
                {
                    CharacterId = _characterId.Value, Title = title,
                    Content = content, Category = category
                });
            await NwTask.SwitchToMainThread();
            if (!result.Success)
            {
                Log.Warn("Codex note save failed: {Error}", result.ErrorMessage);
                if (!_closed) SetStatus("Note was not saved. Your draft is still open.");
                return;
            }
            Guid? savedId = draft.NoteId;
            if (savedId == null && result.Data?.TryGetValue("noteId", out object? value) == true &&
                Guid.TryParse(value.ToString(), out Guid addedId)) savedId = addedId;
            if (savedId is { } saved)
                draft.MarkSaved(saved, title, content, category);
            if (_closed)
            {
                if (!draft.IsDirty && ReferenceEquals(DraftStore?.Value.Get(_player, _characterId.Value), draft))
                    DraftStore?.Value.Remove(_player, _characterId.Value);
                return;
            }
            EndEdit();
            // A saved note must remain visible after changing its category or text.
            _activeCategory = category.ToString();
            SwapCategorySidebar();
            _searchTerm = "";
            _token.SetBindValue(View.NoteSearch, "");
            await ReloadEntriesAsync(savedId);
            if (!_closed) SetStatus("Note saved.");
        }
        finally
        {
            await NwTask.SwitchToMainThread();
            draft.IsSaving = false;
            if (!_closed) SetBusy(false);
        }
    }

    private async Task DeleteNoteAsync()
    {
        if (SelectedNote() is not { CanPlayerEdit: true } note || _characterId == null || Commands?.Value == null) return;
        SetBusy(true);
        try
        {
            CommandResult result = await Commands.Value.DispatchAsync(new DeleteNoteCommand
            {
                CharacterId = _characterId.Value, NoteId = note.Id
            });
            await NwTask.SwitchToMainThread();
            if (_closed) return;
            if (!result.Success)
            {
                Log.Warn("Codex note delete failed: {Error}", result.ErrorMessage);
                SetStatus("Note was not deleted. Please try again.");
                return;
            }
            _selectedNoteId = null;
            await ReloadEntriesAsync();
            if (!_closed) SetStatus("Note deleted.");
        }
        finally
        {
            await NwTask.SwitchToMainThread();
            if (!_closed) SetBusy(false);
        }
    }

    private void ShowSelectedNote()
    {
        CodexNoteEntry? note = SelectedNote();
        _token.SetBindValue(View.ShowNoteActions, note?.CanPlayerEdit == true);
        if (note != null)
        {
            NoteDisplayItem item = new(note);
            SetDetailContent(item.DetailTitle, item.DetailBody);
        }
        else if (_currentEntries.Count == 0)
            SetDetailContent("No Notes", "No notes match this category or search. Use New Note to write one.");
        else
            SetDetailContent("Select a Note", "Choose a note to read it, or use New Note to write one.");
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        _imageInput.Reset();
        _token.SetBindValue(View.CanInteract, !busy);
        _token.SetBindValue(View.ControlColor, busy ? PlayerCodexView.Muted : PlayerCodexView.Gold);
        _token.SetBindValue(View.CanCloseWindow, !busy && _draft == null);
    }

    private void Confirm(string action, string label, string message)
    {
        _pendingAction = action;
        _token.SetBindValue(View.ConfirmLabel, label);
        _token.SetBindValue(View.ShowConfirmation, true);
        SetStatus(message);
    }

    private void ClearConfirmation()
    {
        _pendingAction = null;
        _token.SetBindValue(View.ShowConfirmation, false);
        SetStatus("");
    }

    private void SetStatus(string message) => _token.SetBindValue(View.Status, message);

    // ──────────────────────── Data loading ────────────────────────

    private async Task<List<ICodexDisplayItem>> LoadEntries()
    {
        if (_characterId == null) return new();
        CharacterId cid = _characterId.Value;
        if (_activeTab == CodexTab.Economy) return LoadEconomyEntries(cid);
        if (QueryService?.Value == null) throw new InvalidOperationException("Codex query service unavailable");

        return _activeTab switch
        {
            CodexTab.Knowledge => await LoadLoreEntries(cid),
            CodexTab.Quests => await LoadQuestEntries(cid),
            CodexTab.Notes => await LoadNoteEntries(cid),
            CodexTab.Reputation => await LoadReputationEntries(cid),
            CodexTab.Traits => await LoadTraitEntries(cid),
            _ => new()
        };
    }

    private async Task<List<ICodexDisplayItem>> LoadLoreEntries(CharacterId cid)
    {
        IReadOnlyList<CodexLoreEntry> entries;

        if (_activeCategory == "all")
            entries = await QueryService!.Value.GetAllLoreAsync(cid);
        else if (Enum.TryParse<LoreCategory>(_activeCategory, true, out LoreCategory cat))
            entries = await QueryService!.Value.GetLoreByCategoryAsync(cid, cat);
        else
            entries = await QueryService!.Value.GetAllLoreAsync(cid);

        return entries.Select(e => (ICodexDisplayItem)new LoreDisplayItem(e)).ToList();
    }

    private async Task<List<ICodexDisplayItem>> LoadQuestEntries(CharacterId cid)
    {
        IReadOnlyList<CodexQuestEntry> entries;

        if (_activeCategory == "all")
            entries = await QueryService!.Value.GetAllQuestsAsync(cid);
        else if (Enum.TryParse<QuestState>(_activeCategory, true, out QuestState state))
            entries = await QueryService!.Value.GetQuestsByStateAsync(cid, state);
        else
            entries = await QueryService!.Value.GetAllQuestsAsync(cid);

        return entries.Select(e => (ICodexDisplayItem)new QuestDisplayItem(e)).ToList();
    }

    private async Task<List<ICodexDisplayItem>> LoadNoteEntries(CharacterId cid)
    {
        NoteCategory? category = Enum.TryParse(_activeCategory, true, out NoteCategory cat) ? cat : null;
        IReadOnlyList<CodexNoteEntry> entries = await QueryService!.Value.GetPlayerNotesAsync(cid, category, _searchTerm);
        return entries.Select(e => (ICodexDisplayItem)new NoteDisplayItem(e)).ToList();
    }

    private async Task<List<ICodexDisplayItem>> LoadReputationEntries(CharacterId cid)
    {
        IReadOnlyList<FactionReputation> entries;

        if (_activeCategory == "positive")
            entries = await QueryService!.Value.GetPositiveReputationsAsync(cid);
        else if (_activeCategory == "negative")
            entries = await QueryService!.Value.GetNegativeReputationsAsync(cid);
        else
            entries = await QueryService!.Value.GetAllReputationsAsync(cid);

        return entries.Select(e => (ICodexDisplayItem)new ReputationDisplayItem(e)).ToList();
    }

    private async Task<List<ICodexDisplayItem>> LoadTraitEntries(CharacterId cid)
    {
        IReadOnlyList<CodexTraitEntry> entries;

        if (_activeCategory == "all")
            entries = await QueryService!.Value.GetAllTraitsAsync(cid);
        else if (Enum.TryParse<TraitCategory>(_activeCategory, true, out TraitCategory cat))
            entries = await QueryService!.Value.GetTraitsByCategoryAsync(cid, cat);
        else
            entries = await QueryService!.Value.GetAllTraitsAsync(cid);

        return entries.Select(e => (ICodexDisplayItem)new TraitDisplayItem(e)).ToList();
    }

    private List<ICodexDisplayItem> LoadEconomyEntries(CharacterId cid)
    {
        if (MembershipService?.Value == null) return new List<ICodexDisplayItem>();

        _memberships = MembershipService.Value.GetMemberships(cid);

        List<CharacterKnowledge> knowledge;

        if (_activeCategory == "all")
        {
            knowledge = MembershipService.Value.GetAllCharacterKnowledge(cid);
            _activeMembership = _memberships.FirstOrDefault();
        }
        else
        {
            IndustryMembership? match = _memberships.FirstOrDefault(
                m => string.Equals(m.IndustryTag.Value, _activeCategory, StringComparison.OrdinalIgnoreCase));

            _activeMembership = match;
            knowledge = match != null
                ? MembershipService.Value.GetCharacterKnowledgeForIndustry(cid, _activeCategory)
                : new List<CharacterKnowledge>();
        }

        return knowledge.Select(ck => (ICodexDisplayItem)new KnowledgeDisplayItem(ck)).ToList();
    }

    // ──────────────────────── Entry list refresh ────────────────────────

    private void RefreshEntryList()
    {
        _imageInput.Reset();
        int totalPages = Math.Max(1, (int)Math.Ceiling(_currentEntries.Count / (double)PageSize));
        _currentPage = Math.Clamp(_currentPage, 0, totalPages - 1);
        int startIndex = _currentPage * PageSize;
        int endIndex = Math.Min(startIndex + PageSize, _currentEntries.Count);

        _token.SetBindValue(View.PageInfo, $"{_currentPage + 1} / {totalPages}");
        _token.SetBindValue(View.ShowPrevPage, _currentPage > 0);
        _token.SetBindValue(View.ShowNextPage, _currentPage < totalPages - 1 && _currentEntries.Count > 0);

        for (int i = 0; i < PlayerCodexView.EntriesPerPage; i++)
        {
            int entryIndex = startIndex + i;
            if (i < PageSize && entryIndex < endIndex)
            {
                ICodexDisplayItem item = _currentEntries[entryIndex];
                _token.SetBindValue(View.EntryNames[i], item.DisplayName);
                _token.SetBindValue(View.EntrySubtitles[i], item.Subtitle);
                _token.SetBindValue(View.EntryRowVisible[i], true);
            }
            else
            {
                _token.SetBindValue(View.EntryRowVisible[i], false);
            }
        }
    }

    // ──────────────────────── Entry selection / detail ────────────────────────

    private void SelectEntry(int rowIndex)
    {
        int entryIndex = (_currentPage * PageSize) + rowIndex;
        if (rowIndex < 0 || rowIndex >= PageSize || entryIndex >= _currentEntries.Count) return;

        ICodexDisplayItem item = _currentEntries[entryIndex];
        _selectedNoteId = (item as NoteDisplayItem)?.Note.Id;
        _token.SetBindValue(View.ShowNoteActions, item is NoteDisplayItem note && note.Note.CanPlayerEdit);
        SetDetailContent(item.DetailTitle, item.DetailBody);
    }

    private void SetDetailContent(string title, string body)
    {
        _token.SetBindValue(View.DetailTitle, title);
        _token.SetBindValue(View.DetailBody, body);
    }

    private void SetSelectTraitsVisible(bool visible)
    {
        _token.SetBindValue(View.ShowSelectTraits, visible);
    }

    // ──────────────────────── Category sidebar ────────────────────────

    private void SwapCategorySidebar()
    {
        _imageInput.Reset();
        NuiColumn sidebar = _activeTab switch
        {
            CodexTab.Knowledge => BuildCategoryColumn(
                ("All", "all"),
                ("Arcana", "arcana"),
                ("Arch & Eng", "architectureandengineering"),
                ("Dungeoneering", "dungeoneering"),
                ("Geography", "geography"),
                ("History", "history"),
                ("Local", "local"),
                ("Nature", "nature"),
                ("Nobility", "nobilityandRoyalty"),
                ("Religion", "religion"),
                ("The Planes", "theplanes"),
                ("OOC", "ooc")),

            CodexTab.Quests => BuildCategoryColumn(
                ("All", "all"),
                ("Discovered", "discovered"),
                ("Active", "inprogress"),
                ("Completed", "completed"),
                ("Failed", "failed"),
                ("Abandoned", "abandoned")),

            CodexTab.Notes => BuildCategoryColumn(
                ("All", "all"),
                ("General", "general"),
                ("Quest", "quest"),
                ("Character", "character"),
                ("Location", "location"),
                ("DM Note", "dmnote")),

            CodexTab.Reputation => BuildCategoryColumn(
                ("All", "all"),
                ("Positive", "positive"),
                ("Negative", "negative")),

            CodexTab.Traits => BuildCategoryColumn(
                ("All", "all"),
                ("Background", "background"),
                ("Personality", "personality"),
                ("Physical", "physical"),
                ("Mental", "mental"),
                ("Social", "social"),
                ("Supernatur.", "supernatural"),
                ("Curse", "curse"),
                ("Blessing", "blessing")),

            CodexTab.Economy => BuildEconomyCategoryColumn(),

            _ => BuildCategoryColumn(("All", "all"))
        };

        _token.SetGroupLayout(View.CategoryGroup, sidebar);
    }

    private NuiColumn BuildCategoryColumn(params (string Label, string Id)[] categories) =>
        View.BuildCategoryColumn(_activeTab, _activeCategory, categories);

    private NuiColumn BuildEconomyCategoryColumn()
    {
        if (_characterId == null || MembershipService?.Value == null)
            return BuildCategoryColumn(("All", "all"));

        _memberships = MembershipService.Value.GetMemberships(_characterId.Value);

        List<(string Label, string Id)> categories = new() { ("All", "all") };

        foreach (IndustryMembership m in _memberships)
        {
            string name = IndustryRepository?.Value?.GetByTag(m.IndustryTag)?.Name ?? m.IndustryTag.Value;
            categories.Add((name, m.IndustryTag.Value));
        }

        return BuildCategoryColumn(categories.ToArray());
    }

    // ──────────────────────── Entry list pane swap ────────────────────────

    private void SwapEntryListPane()
    {
        _imageInput.Reset();
        NuiColumn layout = _activeTab switch
        {
            CodexTab.Economy => View.BuildEconomyEntryList(),
            CodexTab.Notes => View.BuildNotesEntryList(),
            _ => View.BuildEntryListInner()
        };

        _token.SetGroupLayout(View.EntryListGroup, layout);
    }

    // ──────────────────────── Proficiency display ────────────────────────

    private void RefreshProficiencyDisplay()
    {
        if (_activeMembership == null)
        {
            _token.SetBindValue(View.ProficiencyLevelText, "No Industry Selected");
            _token.SetBindValue(View.ProficiencyProgressValue, 0f);
            _token.SetBindValue(View.ProficiencyProgressLabel, "");
            return;
        }

        IndustryMembership m = _activeMembership;
        int level = m.ProficiencyXpLevel;
        ProficiencyLevel tier = m.ProficiencyTier;

        _token.SetBindValue(View.ProficiencyLevelText, $"{tier} (Lv. {level})");

        // XpForLevel(0) returns 0 — for Layman/level 0, show progress toward level 1
        int xpNeeded = level < 1 ? ProficiencyXpCurve.XpForLevel(1) : ProficiencyXpCurve.XpForLevel(level);
        if (level >= ProficiencyXpCurve.MaxLevel)
        {
            // Grandmaster / max level
            _token.SetBindValue(View.ProficiencyProgressValue, 1f);
            _token.SetBindValue(View.ProficiencyProgressLabel, "MAX");
        }
        else
        {
            float progress = Math.Clamp(m.ProficiencyXp / (float)xpNeeded, 0f, 1f);
            _token.SetBindValue(View.ProficiencyProgressValue, progress);
            _token.SetBindValue(View.ProficiencyProgressLabel, $"{m.ProficiencyXp} / {xpNeeded} XP");
        }
    }

    // ──────────────────────── CharacterId resolution ────────────────────────

    private CharacterId? ResolveCharacterId()
    {
        try
        {
            NwItem? pcKey = _player.LoginCreature?.Inventory.Items.FirstOrDefault(i => i.ResRef == "ds_pckey");
            if (pcKey == null) return null;

            string dbToken = pcKey.Name.Split("_")[1];
            if (!Guid.TryParse(dbToken, out Guid guid)) return null;

            return CharacterId.From(guid);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to resolve CharacterId for codex");
            return null;
        }
    }
}
