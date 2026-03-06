using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Effects;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Nui;

/// <summary>
///     Presenter for the trait selection window.
///     Routes NUI events, manages category/page/selection state, and pushes updates to the view.
/// </summary>
public sealed class TraitSelectionPresenter : ScryPresenter<TraitSelectionView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    [Inject] private Lazy<TraitSelectionService>? SelectionService { get; init; }
    [Inject] private Lazy<ITraitRepository>? TraitRepository { get; init; }
    [Inject] private Lazy<TraitEffectApplierService>? EffectApplier { get; init; }

    private TraitSelectionModel? _model;
    private int _currentPage;
    private int _selectedTraitIndex = -1;

    public TraitSelectionPresenter(TraitSelectionView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override TraitSelectionView View { get; }
    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _model = new TraitSelectionModel(_player, SelectionService!.Value, TraitRepository!.Value);

        _window = new NuiWindow(View.RootLayout(), "Trait Selection")
        {
            Geometry = new NuiRect(40f, 40f, TraitSelectionView.WindowW, TraitSelectionView.WindowH),
            Resizable = true
        };
    }

    public override void Create()
    {
        if (_window == null)
        {
            _player.SendServerMessage("Trait window not configured.", ColorConstants.Orange);
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
        {
            _player.SendServerMessage("Unable to open trait selection.", ColorConstants.Orange);
            return;
        }

        if (_model?.CharacterId == null)
        {
            _player.SendServerMessage("No character key found. Cannot open trait selection.", ColorConstants.Orange);
            SetDetailContent("Error", "No character key found on your character.");
            return;
        }

        _model.Refresh();
        SwapCategorySidebar();
        RefreshEntryList();
        RefreshBudget();
        SetDetailContent("Select a Trait", "Choose a trait from the list to view its details.");
        HideActionButtons();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType != NuiEventType.Click) return;
        HandleClick(eventData.ElementId);
    }

    public override void Close()
    {
        try
        {
            _token.Close();
        }
        catch
        {
            // ignore
        }
    }

    // ──────────────────── Click routing ────────────────────

    private void HandleClick(string elementId)
    {
        switch (elementId)
        {
            case "btn_confirm":
                ConfirmTraits();
                break;
            case "btn_select_trait":
                SelectCurrentTrait();
                break;
            case "btn_deselect_trait":
                DeselectCurrentTrait();
                break;
            case "btn_prev_page":
                if (_currentPage > 0)
                {
                    _currentPage--;
                    RefreshEntryList();
                }

                break;
            case "btn_next_page":
                int maxPage = Math.Max(0,
                    (_model!.AvailableTraits.Count - 1) / TraitSelectionView.EntriesPerPage);
                if (_currentPage < maxPage)
                {
                    _currentPage++;
                    RefreshEntryList();
                }

                break;
            case "btn_close":
                RaiseCloseEvent();
                Close();
                break;
            default:
                if (elementId.StartsWith("cat_"))
                {
                    ApplyCategory(elementId[4..]);
                }
                else if (elementId.StartsWith("btn_trait_") &&
                         int.TryParse(elementId["btn_trait_".Length..], out int rowIndex))
                {
                    SelectEntry(rowIndex);
                }

                break;
        }
    }

    // ──────────────────── Category ────────────────────

    private void ApplyCategory(string categoryId)
    {
        _model!.ActiveCategory = categoryId == "all"
            ? null
            : Enum.TryParse<TraitCategory>(categoryId, true, out TraitCategory cat)
                ? cat
                : null;

        _currentPage = 0;
        _selectedTraitIndex = -1;
        _model.Refresh();
        RefreshEntryList();
        SetDetailContent("Select a Trait", "Choose a trait from the list to view its details.");
        HideActionButtons();
    }

    // ──────────────────── Entry list ────────────────────

    private void RefreshEntryList()
    {
        List<Trait> traits = _model!.AvailableTraits;
        int totalPages =
            Math.Max(1, (int)Math.Ceiling(traits.Count / (double)TraitSelectionView.EntriesPerPage));
        int startIndex = _currentPage * TraitSelectionView.EntriesPerPage;
        int endIndex = Math.Min(startIndex + TraitSelectionView.EntriesPerPage, traits.Count);

        _token.SetBindValue(View.PageInfo, $"{_currentPage + 1} / {totalPages}");
        _token.SetBindValue(View.ShowPrevPage, _currentPage > 0);
        _token.SetBindValue(View.ShowNextPage, _currentPage < totalPages - 1 && traits.Count > 0);

        for (int i = 0; i < TraitSelectionView.EntriesPerPage; i++)
        {
            int entryIndex = startIndex + i;
            if (entryIndex < endIndex)
            {
                Trait trait = traits[entryIndex];
                bool isSelected = _model.IsTraitSelected(trait.Tag);
                string prefix = isSelected ? "[*] " : "";
                _token.SetBindValue(View.EntryNames[i], $"{prefix}{trait.Name}");
                _token.SetBindValue(View.EntrySubtitles[i],
                    $"{trait.Category.ToString()} | Cost: {trait.PointCost}");
                _token.SetBindValue(View.EntryRowVisible[i], true);
            }
            else
            {
                _token.SetBindValue(View.EntryRowVisible[i], false);
            }
        }
    }

    // ──────────────────── Entry selection / detail ────────────────────

    private void SelectEntry(int rowIndex)
    {
        int entryIndex = (_currentPage * TraitSelectionView.EntriesPerPage) + rowIndex;
        if (entryIndex < 0 || entryIndex >= _model!.AvailableTraits.Count) return;

        _selectedTraitIndex = entryIndex;
        Trait trait = _model.AvailableTraits[entryIndex];
        bool isSelected = _model.IsTraitSelected(trait.Tag);

        string body = trait.Description;

        if (trait.PrerequisiteTraits.Count > 0)
            body += $"\n\nPrerequisites: {string.Join(", ", trait.PrerequisiteTraits)}";
        if (trait.ConflictingTraits.Count > 0)
            body += $"\n\nConflicts with: {string.Join(", ", trait.ConflictingTraits)}";
        if (trait.AllowedRaces.Count > 0)
            body += $"\n\nRaces: {string.Join(", ", trait.AllowedRaces)}";
        if (trait.AllowedClasses.Count > 0)
            body += $"\n\nClasses: {string.Join(", ", trait.AllowedClasses)}";

        body += $"\n\nCost: {trait.PointCost} point(s)";

        SetDetailContent(trait.Name, body);

        if (isSelected)
        {
            // Can only deselect if not confirmed
            CharacterTrait? ct = _model.SelectedTraits.FirstOrDefault(c => c.TraitTag.Value == trait.Tag);
            bool canDeselect = ct != null && !ct.IsConfirmed;
            _token.SetBindValue(View.ShowSelectButton, false);
            _token.SetBindValue(View.ShowDeselectButton, canDeselect);
        }
        else
        {
            _token.SetBindValue(View.ShowSelectButton, true);
            _token.SetBindValue(View.ShowDeselectButton, false);
        }
    }

    // ──────────────────── Trait actions ────────────────────

    private void SelectCurrentTrait()
    {
        if (_selectedTraitIndex < 0 || _selectedTraitIndex >= _model!.AvailableTraits.Count) return;

        Trait trait = _model.AvailableTraits[_selectedTraitIndex];
        ICharacterInfo charInfo = BuildCharacterInfo();

        if (_model.SelectTrait(trait.Tag, charInfo))
        {
            _player.SendServerMessage($"Trait '{trait.Name}' selected.".ColorString(ColorConstants.Green));
            RefreshEntryList();
            RefreshBudget();
            SelectEntry(_selectedTraitIndex - (_currentPage * TraitSelectionView.EntriesPerPage));
        }
        else
        {
            _player.SendServerMessage(
                $"Cannot select '{trait.Name}'. Check eligibility, conflicts, or budget."
                    .ColorString(ColorConstants.Orange));
        }
    }

    private void DeselectCurrentTrait()
    {
        if (_selectedTraitIndex < 0 || _selectedTraitIndex >= _model!.AvailableTraits.Count) return;

        Trait trait = _model.AvailableTraits[_selectedTraitIndex];

        if (_model.DeselectTrait(trait.Tag))
        {
            _player.SendServerMessage($"Trait '{trait.Name}' removed.".ColorString(ColorConstants.Green));
            RefreshEntryList();
            RefreshBudget();
            SelectEntry(_selectedTraitIndex - (_currentPage * TraitSelectionView.EntriesPerPage));
        }
        else
        {
            _player.SendServerMessage(
                "Cannot remove a confirmed trait.".ColorString(ColorConstants.Orange));
        }
    }

    private void ConfirmTraits()
    {
        if (_model!.ConfirmTraits())
        {
            _player.SendServerMessage("Traits confirmed!".ColorString(ColorConstants.Green));

            // Apply trait effects immediately after confirmation
            EffectApplier?.Value.ApplyTraits(_player);

            RefreshEntryList();
            RefreshBudget();
            HideActionButtons();
        }
        else
        {
            _player.SendServerMessage(
                "Cannot confirm — you are over budget.".ColorString(ColorConstants.Orange));
        }
    }

    // ──────────────────── View helpers ────────────────────

    private void RefreshBudget()
    {
        TraitBudget budget = _model!.Budget;
        _token.SetBindValue(View.BudgetLabel,
            $"Trait Points: {budget.AvailablePoints} / {budget.TotalPoints} available  ({budget.SpentPoints} spent)");
    }

    private void SetDetailContent(string title, string body)
    {
        _token.SetBindValue(View.DetailTitle, title);
        _token.SetBindValue(View.DetailBody, body);
    }

    private void HideActionButtons()
    {
        _token.SetBindValue(View.ShowSelectButton, false);
        _token.SetBindValue(View.ShowDeselectButton, false);
    }

    // ──────────────────── Category sidebar ────────────────────

    private void SwapCategorySidebar()
    {
        NuiColumn sidebar = BuildCategoryColumn(
            ("All", "all"),
            ("Background", "background"),
            ("Personality", "personality"),
            ("Physical", "physical"),
            ("Mental", "mental"),
            ("Social", "social"),
            ("Supernatur.", "supernatural"),
            ("Curse", "curse"),
            ("Blessing", "blessing"));

        _token.SetGroupLayout(View.CategoryGroup, sidebar);
    }

    private static NuiColumn BuildCategoryColumn(params (string Label, string Id)[] categories)
    {
        List<NuiElement> children =
        [
            new NuiLabel("Categories")
            {
                Height = 28f,
                HorizontalAlign = NuiHAlign.Center,
                VerticalAlign = NuiVAlign.Middle
            },
            new NuiSpacer { Height = 4f }
        ];

        foreach ((string label, string id) in categories)
        {
            children.Add(new NuiButton(label)
            {
                Id = $"cat_{id}",
                Width = 120f,
                Height = 28f,
                Tooltip = $"Show {label.ToLower()} traits"
            });
            children.Add(new NuiSpacer { Height = 2f });
        }

        children.Add(new NuiSpacer());
        return new NuiColumn { Children = children };
    }

    // ──────────────────── ICharacterInfo adapter ────────────────────

    private ICharacterInfo BuildCharacterInfo()
    {
        NwCreature? creature = _player.LoginCreature;

        string raceName = creature?.Race.Name.ToString() ?? "Unknown";

        List<CharacterClassData> classes = [];
        if (creature != null)
        {
            foreach (CreatureClassInfo classInfo in creature.Classes)
            {
                classes.Add(CharacterClassData.From(classInfo.Class.Name.ToString(), classInfo.Level));
            }
        }

        return new NwCharacterInfo(RaceData.From(raceName), classes);
    }

    private sealed class NwCharacterInfo(RaceData race, IReadOnlyList<CharacterClassData> classes) : ICharacterInfo
    {
        public RaceData Race => race;
        public IReadOnlyList<CharacterClassData> Classes => classes;
    }
}
