using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Effects;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Newtonsoft.Json;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Nui;

/// <summary>Routes graphical trait input through the existing selection model and service.</summary>
public sealed class TraitSelectionPresenter : ScryPresenter<TraitSelectionView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly NwPlayer _player;
    private readonly NuiImageInput _imageInput = new();
    private readonly NuiBind<NuiRect> _geometry = new("trait_geometry");
    private readonly string?[] _displayedRowTags = new string?[TraitSelectionView.EntriesPerPage];
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private NuiRect _expandedGeometry;
    private TraitSelectionModel? _model;
    private ImageContext? _pressedContext;
    private int _currentPage;
    private int _revision;
    private string? _viewedTraitTag;
    private bool _hideDetailActions = true;
    private bool _removeAction;
    private bool _compact;
    private bool _busy;
    private bool _closed;

    [Inject] private Lazy<TraitSelectionService>? SelectionService { get; init; }
    [Inject] private Lazy<ITraitRepository>? TraitRepository { get; init; }
    [Inject] private Lazy<TraitEffectApplierService>? EffectApplier { get; init; }
    [Inject] private DevicePropertyService DevicePropertyService { get; init; } = null!;

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
        float scale = DevicePropertyService.GetGuiScale(_player) / 100f;
        if (scale <= 0) scale = 1;
        View.SetScaleFactor(scale);
        _expandedGeometry = new NuiRect(40 / scale, 40 / scale, View.Graphical.WindowWidth, View.Graphical.WindowHeight(false));
        _window = new NuiWindow(View.RootLayout(), null!)
        {
            Geometry = _geometry, Transparent = true, Border = false, Resizable = false,
            Closable = false, Collapsed = false
        };
    }

    public override void Create()
    {
        if (_window == null || !_player.TryCreateNuiWindow(_window, out _token))
            throw new InvalidOperationException("Unable to open trait selection.");
        _token.SetBindValue(_geometry, _expandedGeometry);
        if (_model?.CharacterId == null)
        {
            _player.SendServerMessage("No character key found. Cannot open trait selection.", ColorConstants.Orange);
            PaintView();
            return;
        }
        _model.Refresh();
        PaintView();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (_closed || eventData.EventType is not (NuiEventType.MouseDown or NuiEventType.MouseUp)) return;
        bool image = View.Graphical.ImageActionIds.Contains(eventData.ElementId);
        int? button = null;
        if (image)
        {
            try { button = eventData.GetEventPayload<NuiMousePayload>()?.MouseButton; }
            catch (JsonException) { /* Invalid buttons must not become left-button actions. */ }
        }
        if (HandleImageEvent(eventData.EventType, eventData.ElementId, button, image,
                image && IsImageActionEnabled(eventData.ElementId), GetImageContext(eventData.ElementId)))
            HandleAction(eventData.ElementId);
    }

    internal readonly record struct ImageContext(Guid? CharacterId, string? Category, int Page,
        string? ViewedTag, string? RowTag, int Revision, bool Compact);
    internal readonly record struct ActionAvailability(bool Closed, bool Busy, bool HasCharacter, bool Compact,
        int Page, int Count, bool CanSelect, bool CanRemove);

    internal bool HandleImageEvent(NuiEventType type, string id, int? button, bool image, bool enabled, ImageContext? context)
    {
        if (_closed || type is not (NuiEventType.MouseDown or NuiEventType.MouseUp)) return false;
        if (type == NuiEventType.MouseDown && image) _pressedContext = enabled && button == 0 ? context : null;
        if (type == NuiEventType.MouseUp) enabled &= _pressedContext != null && _pressedContext == context;
        bool activate = _imageInput.Handle(type, id, button, enabled, image);
        if (type == NuiEventType.MouseUp) _pressedContext = null;
        return activate;
    }

    private ImageContext GetImageContext(string id) => new(_model?.CharacterId?.Value, _model?.ActiveCategory?.ToString(),
        _currentPage, _viewedTraitTag, DisplayedTrait(id)?.Tag, _revision, _compact);

    private void InvalidateInput()
    {
        _imageInput.Reset();
        _pressedContext = null;
        _revision++;
    }

    private Trait? ViewedTrait() => _model == null ? null : FindViewedTrait(_model.AvailableTraits, _viewedTraitTag);
    private Trait? DisplayedTrait(string id) => _model == null ? null : GetDisplayedTrait(id, _displayedRowTags, _model.AvailableTraits, _currentPage);
    private ActionAvailability Availability()
    {
        Trait? viewed = ViewedTrait();
        CharacterTrait? acquired = _model?.SelectedTraits.FirstOrDefault(trait => trait.TraitTag.Value == viewed?.Tag);
        return new(_closed, _busy, _model?.CharacterId != null && _player.LoginCreature is { IsValid: true }, _compact,
            _currentPage, _model?.AvailableTraits.Count ?? 0,
            !_hideDetailActions && viewed != null && acquired == null,
            !_hideDetailActions && viewed != null && acquired is { IsConfirmed: false });
    }

    private bool IsImageActionEnabled(string id)
    {
        if (!CanActivateImageAction(id, Availability())) return false;
        return !id.StartsWith("btn_trait_", StringComparison.Ordinal) || DisplayedTrait(id) != null;
    }

    internal static bool CanActivateImageAction(string id, ActionAvailability state)
    {
        if (state.Closed || state.Busy) return false;
        if (id is "btn_close" or "trait_collapse") return true;
        if (!state.HasCharacter || state.Compact) return false;
        return id switch
        {
            "btn_confirm" => true,
            "btn_select_trait" => state.CanSelect,
            "btn_deselect_trait" => state.CanRemove,
            "btn_prev_page" => state.Page > 0 && state.Page == ClampPage(state.Page, state.Count),
            "btn_next_page" => state.Page >= 0 && state.Page < ClampPage(int.MaxValue, state.Count),
            _ => TraitSelectionGraphicalView.Categories.Any(category => id == $"cat_{category.Id}") || RowIndex(id, state.Page, state.Count) != null
        };
    }

    internal static int ClampPage(int page, int count) => Math.Clamp(page, 0, Math.Max(0, (count - 1) / TraitSelectionView.EntriesPerPage));
    internal static int? RowIndex(string id, int page, int count)
    {
        const string prefix = "btn_trait_";
        if (!id.StartsWith(prefix, StringComparison.Ordinal) || !int.TryParse(id[prefix.Length..], out int slot) ||
            slot < 0 || slot >= TraitSelectionView.EntriesPerPage || page < 0 || count <= 0 || page != ClampPage(page, count)) return null;
        int index = page * TraitSelectionView.EntriesPerPage + slot;
        return index < count ? index : null;
    }
    internal static Trait? GetDisplayedTrait(string id, IReadOnlyList<string?> displayed, IReadOnlyList<Trait> traits, int page)
    {
        if (RowIndex(id, page, traits.Count) is not { } index) return null;
        int slot = index - page * TraitSelectionView.EntriesPerPage;
        return displayed[slot] == traits[index].Tag ? traits[index] : null;
    }
    internal static Trait? FindViewedTrait(IReadOnlyList<Trait> traits, string? tag) =>
        tag == null ? null : traits.FirstOrDefault(trait => trait.Tag == tag);

    public override void UpdateView()
    {
        if (_closed || _busy) return;
        InvalidateInput();
        _model?.Refresh();
        PaintView();
    }

    public override void Close()
    {
        if (_closed) return;
        _closed = true;
        InvalidateInput();
        try { _token.Close(); }
        catch { /* The client/director may already have closed the token. */ }
    }

    private void HandleAction(string id)
    {
        if (!IsImageActionEnabled(id)) return;
        InvalidateInput();
        bool mutation = id is "btn_select_trait" or "btn_deselect_trait" or "btn_confirm";
        try
        {
            if (mutation) { _busy = true; ApplyControlState(); }
            switch (id)
            {
                case "btn_close": RaiseCloseEvent(); Close(); return;
                case "trait_collapse": SetCompact(!_compact); return;
                case "btn_prev_page": _currentPage--; PaintView(); return;
                case "btn_next_page": _currentPage++; PaintView(); return;
                case "btn_select_trait": SelectCurrentTrait(); return;
                case "btn_deselect_trait": DeselectCurrentTrait(); return;
                case "btn_confirm": ConfirmTraits(); return;
            }
            if (id.StartsWith("cat_", StringComparison.Ordinal)) ApplyCategory(id[4..]);
            else if (DisplayedTrait(id) is { } trait)
            {
                _viewedTraitTag = trait.Tag;
                _hideDetailActions = false;
                PaintView();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to handle trait image action '{ElementId}'", id);
            if (!_closed) _player.SendServerMessage("Could not complete the trait menu action. Please contact a DM.", ColorConstants.Orange);
        }
        finally
        {
            if (mutation)
            {
                _busy = false;
                InvalidateInput();
                if (!_closed) ApplyControlState();
            }
        }
    }

    private void ApplyCategory(string id)
    {
        _model!.ActiveCategory = id == "all" ? null : Enum.Parse<TraitCategory>(id, true);
        _currentPage = 0;
        _viewedTraitTag = null;
        _hideDetailActions = true;
        _model.Refresh();
        PaintView();
    }

    private void SelectCurrentTrait()
    {
        if (ViewedTrait() is not { } trait) return;
        bool success = _model!.SelectTrait(trait.Tag, BuildCharacterInfo());
        _player.SendServerMessage(success ? $"Trait '{trait.Name}' selected.".ColorString(ColorConstants.Green)
            : $"Cannot select '{trait.Name}'. Check eligibility, conflicts, or budget.".ColorString(ColorConstants.Orange));
        if (!success) _model.Refresh();
        PaintView();
    }

    private void DeselectCurrentTrait()
    {
        if (ViewedTrait() is not { } trait) return;
        bool success = _model!.DeselectTrait(trait.Tag);
        _player.SendServerMessage(success ? $"Trait '{trait.Name}' removed.".ColorString(ColorConstants.Green)
            : "Cannot remove a confirmed trait.".ColorString(ColorConstants.Orange));
        if (!success) _model.Refresh();
        PaintView();
    }

    private void ConfirmTraits()
    {
        if (_model!.ConfirmTraits())
        {
            _player.SendServerMessage("Traits confirmed!".ColorString(ColorConstants.Green));
            EffectApplier?.Value.ApplyTraits(_player);
            _hideDetailActions = true;
        }
        else
        {
            _player.SendServerMessage("Cannot confirm — you are over budget.".ColorString(ColorConstants.Orange));
            _model.Refresh();
        }
        PaintView();
    }

    private void SetCompact(bool compact)
    {
        if (compact) _expandedGeometry = _token.GetBindValue(_geometry);
        _compact = compact;
        _token.SetGroupLayout(View.LayoutHost, View.Graphical.BuildLayout(compact, _removeAction));
        _token.SetBindValue(_geometry, new NuiRect(_expandedGeometry.X, _expandedGeometry.Y,
            View.Graphical.WindowWidth, View.Graphical.WindowHeight(compact)));
        PaintView();
    }

    private void PaintView()
    {
        if (_closed) return;
        InvalidateInput();
        bool valid = _model?.CharacterId != null;
        List<Trait> traits = valid ? _model!.AvailableTraits : [];
        _currentPage = ClampPage(_currentPage, traits.Count);
        Trait? viewed = FindViewedTrait(traits, _viewedTraitTag);
        if (viewed == null) { _viewedTraitTag = null; _hideDetailActions = true; }
        TraitBudget budget = _model?.Budget ?? TraitBudget.CreateDefault();
        _token.SetBindValue(View.Graphical.HeaderTitle, "Trait Selection");
        _token.SetBindValue(View.Graphical.CollapseGlyph, _compact ? "ui_cdx_i_up" : "ui_cdx_i_down");
        _token.SetBindValue(View.BudgetLabel, $"Trait Points: {budget.AvailablePoints} / {budget.TotalPoints} available  ({budget.SpentPoints} spent)");
        int pages = Math.Max(1, (traits.Count + TraitSelectionView.EntriesPerPage - 1) / TraitSelectionView.EntriesPerPage);
        _token.SetBindValue(View.PageInfo, $"{_currentPage + 1} / {pages}");
        _token.SetBindValue(View.ShowPrevPage, _currentPage > 0);
        _token.SetBindValue(View.ShowNextPage, _currentPage < pages - 1);
        for (int i = 0; i < TraitSelectionGraphicalView.Categories.Length; i++)
        {
            string active = _model?.ActiveCategory?.ToString().ToLowerInvariant() ?? "all";
            _token.SetBindValue(View.Graphical.CategoryTextures[i], TraitSelectionGraphicalView.Categories[i].Id == active
                ? "ui_cdx_cat_s_v2" : "ui_cdx_cat_n_v2");
        }
        for (int i = 0; i < TraitSelectionView.EntriesPerPage; i++)
        {
            int index = _currentPage * TraitSelectionView.EntriesPerPage + i;
            Trait? trait = index < traits.Count ? traits[index] : null;
            _displayedRowTags[i] = trait?.Tag;
            string subtitle = trait == null ? "" : $"{trait.Category} | Cost: {trait.PointCost}";
            _token.SetBindValue(View.EntryRowVisible[i], trait != null);
            _token.SetBindValue(View.EntryNames[i], trait == null ? "" : (_model!.IsTraitSelected(trait.Tag) ? "[*] " : "") + trait.Name);
            _token.SetBindValue(View.EntrySubtitles[i], subtitle);
            _token.SetBindValue(View.Graphical.EntryTooltips[i], trait == null ? "" : $"{trait.Name}\n{subtitle}");
            _token.SetBindValue(View.Graphical.EntryTextures[i], trait != null && trait.Tag == _viewedTraitTag ? "ui_trs_ent_s" : "ui_trs_ent_n");
        }
        _token.SetBindValue(View.DetailTitle, viewed?.Name ?? (valid ? "Select a Trait" : "Error"));
        _token.SetBindValue(View.DetailBody, viewed != null ? DetailBody(viewed)
            : valid ? "Choose a trait from the list to view its details." : "No character key found on your character.");
        ActionAvailability state = Availability();
        _token.SetBindValue(View.ShowSelectButton, state.CanSelect);
        _token.SetBindValue(View.ShowDeselectButton, state.CanRemove);
        if (_removeAction != state.CanRemove)
        {
            _removeAction = state.CanRemove;
            if (!_compact) _token.SetGroupLayout(View.Graphical.DetailActionSlot, View.Graphical.BuildDetailActionLayout(_removeAction));
        }
        ApplyControlState();
    }

    private void ApplyControlState()
    {
        ActionAvailability state = Availability();
        bool header = !state.Closed && !state.Busy;
        bool controls = header && state.HasCharacter && !state.Compact;
        _token.SetBindValue(View.Graphical.HeaderEnabled, header);
        _token.SetBindValue(View.Graphical.HeaderColor, header ? TraitSelectionGraphicalView.Gold : TraitSelectionGraphicalView.Muted);
        _token.SetBindValue(View.Graphical.ControlsEnabled, controls);
        _token.SetBindValue(View.Graphical.ControlsColor, controls ? TraitSelectionGraphicalView.Gold : TraitSelectionGraphicalView.Muted);
    }

    private static string DetailBody(Trait trait)
    {
        string body = trait.Description;
        if (trait.PrerequisiteTraits.Count > 0) body += $"\n\nPrerequisites: {string.Join(", ", trait.PrerequisiteTraits)}";
        if (trait.ConflictingTraits.Count > 0) body += $"\n\nConflicts with: {string.Join(", ", trait.ConflictingTraits)}";
        if (trait.AllowedRaces.Count > 0) body += $"\n\nRaces: {string.Join(", ", trait.AllowedRaces)}";
        if (trait.AllowedClasses.Count > 0) body += $"\n\nClasses: {string.Join(", ", trait.AllowedClasses)}";
        return body + $"\n\nCost: {trait.PointCost} point(s)";
    }

    private ICharacterInfo BuildCharacterInfo()
    {
        NwCreature? creature = _player.LoginCreature;
        string raceName = creature?.Race.Name ?? "Unknown";
        List<CharacterClassData> classes = [];
        if (creature != null)
            foreach (CreatureClassInfo classInfo in creature.Classes)
                classes.Add(CharacterClassData.From(classInfo.Class.Name.ToString(), classInfo.Level));
        return new NwCharacterInfo(RaceData.From(raceName), classes);
    }
    private sealed class NwCharacterInfo(RaceData race, IReadOnlyList<CharacterClassData> classes) : ICharacterInfo
    {
        public RaceData Race => race;
        public IReadOnlyList<CharacterClassData> Classes => classes;
    }
}
