using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

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

    // Injected services
    [Inject] private Lazy<CodexQueryService>? QueryService { get; init; }

    // State
    private CodexTab _activeTab = CodexTab.Knowledge;
    private string _activeCategory = "all";
    private int _currentPage;
    private int _selectedIndex = -1;
    private List<ICodexDisplayItem> _currentEntries = new();
    private CharacterId? _characterId;

    public PlayerCodexPresenter(PlayerCodexView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override PlayerCodexView View { get; }
    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), "Codex")
        {
            Geometry = new NuiRect(40f, 40f, PlayerCodexView.WindowW, PlayerCodexView.WindowH),
            Resizable = true
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

        if (_characterId == null)
        {
            _player.SendServerMessage("No character key found. Cannot open codex.", ColorConstants.Orange);
            SetDetailContent("Error", "No character key found on your character.");
            return;
        }

        // Start on Knowledge tab
        SwitchTab(CodexTab.Knowledge);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                HandleClick(eventData.ElementId);
                break;
        }
    }

    public override void UpdateView()
    {
        // Triggered externally if needed; main state updates happen through SwitchTab/ApplyCategory/SelectEntry
    }

    public override void Close()
    {
        try { _token.Close(); }
        catch { /* ignore */ }
    }

    // ──────────────────────── Click routing ────────────────────────

    private void HandleClick(string elementId)
    {
        switch (elementId)
        {
            case "tab_knowledge":
                SwitchTab(CodexTab.Knowledge);
                break;
            case "tab_quests":
                SwitchTab(CodexTab.Quests);
                break;
            case "tab_notes":
                SwitchTab(CodexTab.Notes);
                break;
            case "tab_reputation":
                SwitchTab(CodexTab.Reputation);
                break;

            case "btn_prev_page":
                if (_currentPage > 0) { _currentPage--; RefreshEntryList(); }
                break;
            case "btn_next_page":
                int maxPage = Math.Max(0, (_currentEntries.Count - 1) / PlayerCodexView.EntriesPerPage);
                if (_currentPage < maxPage) { _currentPage++; RefreshEntryList(); }
                break;

            case "codex_close":
                RaiseCloseEvent();
                Close();
                break;

            default:
                // Category buttons: cat_{category}
                if (elementId.StartsWith("cat_"))
                {
                    string category = elementId.Substring(4);
                    ApplyCategory(category);
                }
                // Entry buttons: btn_entry_{i}
                else if (elementId.StartsWith("btn_entry_"))
                {
                    string indexStr = elementId.Substring("btn_entry_".Length);
                    if (int.TryParse(indexStr, out int rowIndex))
                        SelectEntry(rowIndex);
                }
                break;
        }
    }

    // ──────────────────────── Tab switching ────────────────────────

    private async void SwitchTab(CodexTab tab)
    {
        _activeTab = tab;
        _activeCategory = "all";
        _currentPage = 0;
        _selectedIndex = -1;

        SwapCategorySidebar();
        await LoadEntries();
        await NwTask.SwitchToMainThread();
        RefreshEntryList();
        SetDetailContent("Select an Entry", "Choose an entry from the list to view its details.");
    }

    // ──────────────────────── Category filtering ────────────────────────

    private async void ApplyCategory(string category)
    {
        _activeCategory = category;
        _currentPage = 0;
        _selectedIndex = -1;

        await LoadEntries();
        await NwTask.SwitchToMainThread();
        RefreshEntryList();
        SetDetailContent("Select an Entry", "Choose an entry from the list to view its details.");
    }

    // ──────────────────────── Data loading ────────────────────────

    private async Task LoadEntries()
    {
        if (_characterId == null || QueryService?.Value == null)
        {
            _currentEntries = new List<ICodexDisplayItem>();
            return;
        }

        CharacterId cid = _characterId.Value;

        switch (_activeTab)
        {
            case CodexTab.Knowledge:
                _currentEntries = await LoadLoreEntries(cid);
                break;
            case CodexTab.Quests:
                _currentEntries = await LoadQuestEntries(cid);
                break;
            case CodexTab.Notes:
                _currentEntries = await LoadNoteEntries(cid);
                break;
            case CodexTab.Reputation:
                _currentEntries = await LoadReputationEntries(cid);
                break;
        }
    }

    private async Task<List<ICodexDisplayItem>> LoadLoreEntries(CharacterId cid)
    {
        IReadOnlyList<CodexLoreEntry> entries;

        if (_activeCategory == "all")
            entries = await QueryService!.Value.GetAllLoreAsync(cid);
        else if (Enum.TryParse<LoreTier>(_activeCategory, true, out LoreTier tier))
            entries = await QueryService!.Value.GetLoreByTierAsync(cid, tier);
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
        IReadOnlyList<CodexNoteEntry> entries;

        if (_activeCategory == "all")
            entries = await QueryService!.Value.GetAllNotesAsync(cid);
        else if (Enum.TryParse<NoteCategory>(_activeCategory, true, out NoteCategory cat))
            entries = await QueryService!.Value.GetNotesByCategoryAsync(cid, cat);
        else
            entries = await QueryService!.Value.GetAllNotesAsync(cid);

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

    // ──────────────────────── Entry list refresh ────────────────────────

    private void RefreshEntryList()
    {
        int totalPages = Math.Max(1, (int)Math.Ceiling(_currentEntries.Count / (double)PlayerCodexView.EntriesPerPage));
        int startIndex = _currentPage * PlayerCodexView.EntriesPerPage;
        int endIndex = Math.Min(startIndex + PlayerCodexView.EntriesPerPage, _currentEntries.Count);

        _token.SetBindValue(View.PageInfo, $"{_currentPage + 1} / {totalPages}");
        _token.SetBindValue(View.ShowPrevPage, _currentPage > 0);
        _token.SetBindValue(View.ShowNextPage, _currentPage < totalPages - 1 && _currentEntries.Count > 0);

        for (int i = 0; i < PlayerCodexView.EntriesPerPage; i++)
        {
            int entryIndex = startIndex + i;
            if (entryIndex < endIndex)
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
        int entryIndex = (_currentPage * PlayerCodexView.EntriesPerPage) + rowIndex;
        if (entryIndex < 0 || entryIndex >= _currentEntries.Count) return;

        _selectedIndex = entryIndex;
        ICodexDisplayItem item = _currentEntries[entryIndex];
        SetDetailContent(item.DetailTitle, item.DetailBody);
    }

    private void SetDetailContent(string title, string body)
    {
        _token.SetBindValue(View.DetailTitle, title);
        _token.SetBindValue(View.DetailBody, body);
    }

    // ──────────────────────── Category sidebar ────────────────────────

    private void SwapCategorySidebar()
    {
        NuiColumn sidebar = _activeTab switch
        {
            CodexTab.Knowledge => BuildCategoryColumn(
                ("All", "all"),
                ("Common", "common"),
                ("Uncommon", "uncommon"),
                ("Rare", "rare"),
                ("Legendary", "legendary")),

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

            _ => BuildCategoryColumn(("All", "all"))
        };

        _token.SetGroupLayout(View.CategoryGroup, sidebar);
    }

    private NuiColumn BuildCategoryColumn(params (string Label, string Id)[] categories)
    {
        List<NuiElement> children = new()
        {
            new NuiLabel(_activeTab.ToString())
            {
                Height = 28f,
                HorizontalAlign = NuiHAlign.Center,
                VerticalAlign = NuiVAlign.Middle
            },
            new NuiSpacer { Height = 4f }
        };

        foreach ((string label, string id) in categories)
        {
            children.Add(new NuiButton(label)
            {
                Id = $"cat_{id}",
                Width = 120f,
                Height = 28f,
                Tooltip = $"Show {label.ToLower()}"
            });
            children.Add(new NuiSpacer { Height = 2f });
        }

        children.Add(new NuiSpacer());

        return new NuiColumn { Children = children };
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
