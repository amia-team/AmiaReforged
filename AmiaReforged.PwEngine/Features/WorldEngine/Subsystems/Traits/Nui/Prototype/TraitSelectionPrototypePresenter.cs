using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Nui.Prototype;

/// <summary>Visual-only trait shell; never creates a selection model or performs trait actions.</summary>
public sealed class TraitSelectionPrototypePresenter(NwPlayer player, DevicePropertyService device, string scenario,
    bool corner, int? traitCount = null) : ScryPresenter<TraitSelectionGraphicalView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly NuiBind<NuiRect> _geometry = new("trait_preview_geometry");
    private readonly TraitSelectionPreviewData _sample = TraitSelectionPreviewData.Create(scenario, traitCount);
    private NuiWindow _window = null!;
    private NuiGroup _host = null!;
    private NuiWindowToken _token;
    private NuiRect _expandedGeometry;
    private float _scaleFactor;
    private bool _compact = scenario == "compact";
    private bool _closed;
    public override TraitSelectionGraphicalView View { get; } = new();
    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _scaleFactor = device.GetGuiScale(player) / 100f;
        if (_scaleFactor <= 0) _scaleFactor = 1;
        View.SetScaleFactor(_scaleFactor);
        _host = new NuiGroup
        {
            Id = "trait_preview_host", Width = View.WindowWidth,
            Margin = 0, Padding = 0, Border = false, Scrollbars = NuiScrollbars.None, Scissor = true,
            Element = View.BuildLayout(_compact, _sample.ShowRemove)
        };
        _window = new NuiWindow(_host, null!)
        {
            Geometry = _geometry, Transparent = true, Border = false, Resizable = false,
            Closable = false, Collapsed = false
        };
    }

    public override void Create()
    {
        if (!player.TryCreateNuiWindow(_window, out _token))
            throw new InvalidOperationException("The client could not create the trait preview window.");
        _expandedGeometry = new NuiRect(corner ? 40 / _scaleFactor : -1, corner ? 40 / _scaleFactor : -1,
            View.WindowWidth, View.WindowHeight(false));
        _token.SetBindValue(_geometry, new NuiRect(_expandedGeometry.X, _expandedGeometry.Y,
            View.WindowWidth, View.WindowHeight(_compact)));
        UpdateView();
        if (scenario == "collapse") _ = CycleCollapseAsync();
        player.SendServerMessage($"Trait preview / {scenario}: {device.GetGuiWidth(player)}x{device.GetGuiHeight(player)}, " +
            $"GUI {device.GetGuiScale(player)}%, physical shell {TraitSelectionGraphicalView.BaseWindowW:0.#}x{TraitSelectionGraphicalView.BaseWindowH:0.#}. " +
            "Images are visual-only. Repeat ./traitsui to close.", ColorConstants.Cyan);
    }

    public override void UpdateView()
    {
        if (_closed) return;
        _token.SetBindValue(View.HeaderTitle, "Trait Selection");
        _token.SetBindValue(View.BudgetLabel, $"Trait Points: {_sample.Budget.AvailablePoints} / {_sample.Budget.TotalPoints} available  ({_sample.Budget.SpentPoints} spent)");
        _token.SetBindValue(View.DetailTitle, _sample.DetailTitle);
        _token.SetBindValue(View.DetailBody, _sample.DetailBody);
        _token.SetBindValue(View.ShowSelectButton, _sample.ShowSelect);
        _token.SetBindValue(View.ShowDeselectButton, _sample.ShowRemove);
        _token.SetBindValue(View.ControlsEnabled, _sample.Enabled);
        _token.SetBindValue(View.HeaderEnabled, _sample.Enabled);
        _token.SetBindValue(View.HeaderColor, _sample.Enabled ? TraitSelectionGraphicalView.Gold : TraitSelectionGraphicalView.Muted);
        _token.SetBindValue(View.ControlsColor, _sample.Enabled ? TraitSelectionGraphicalView.Gold : TraitSelectionGraphicalView.Muted);
        _token.SetBindValue(View.CollapseGlyph, _compact ? "ui_cdx_i_up" : "ui_cdx_i_down");
        _token.SetBindValue(View.PageInfo, $"{_sample.Page + 1} / {_sample.PageCount}");
        _token.SetBindValue(View.ShowPrevPage, _sample.Page > 0);
        _token.SetBindValue(View.ShowNextPage, _sample.Page < _sample.PageCount - 1);
        for (int i = 0; i < TraitSelectionGraphicalView.Categories.Length; i++)
            _token.SetBindValue(View.CategoryTextures[i], TraitSelectionGraphicalView.Categories[i].Id == _sample.ActiveCategory
                ? "ui_cdx_cat_s_v2" : "ui_cdx_cat_n_v2");
        for (int i = 0; i < TraitSelectionGraphicalView.EntriesPerPage; i++)
        {
            int index = _sample.Page * TraitSelectionGraphicalView.EntriesPerPage + i;
            bool visible = index < _sample.Traits.Count;
            TraitPreviewRow? row = visible ? _sample.Traits[index] : null;
            _token.SetBindValue(View.EntryRowVisible[i], visible);
            _token.SetBindValue(View.EntryNames[i], row == null ? "" : (row.Acquired ? "[*] " : "") + row.Name);
            _token.SetBindValue(View.EntrySubtitles[i], row == null ? "" : $"{row.Category} | Cost: {row.Cost}");
            _token.SetBindValue(View.EntryTooltips[i], row == null ? "" : $"{row.Name}\n{row.Category} | Cost: {row.Cost}");
            _token.SetBindValue(View.EntryTextures[i], visible && index == _sample.ViewedIndex ? "ui_trs_ent_s" : "ui_trs_ent_n");
        }
    }

    private async Task CycleCollapseAsync()
    {
        try
        {
            await NwTask.Delay(TimeSpan.FromSeconds(3));
            await NwTask.SwitchToMainThread();
            if (_closed || player.LoginCreature is not { IsValid: true }) return;
            SetCompact(true);
            player.SendServerMessage("Trait preview: compact header for three seconds; the same window and sample state will be restored.", ColorConstants.Cyan);
            await NwTask.Delay(TimeSpan.FromSeconds(3));
            await NwTask.SwitchToMainThread();
            if (_closed || player.LoginCreature is not { IsValid: true }) return;
            SetCompact(false);
            player.SendServerMessage("Trait preview restored. Check category, viewed/acquired states, budget and action position.", ColorConstants.Cyan);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not run trait preview collapse/restore cycle");
        }
    }

    private void SetCompact(bool compact)
    {
        if (compact == _compact || _closed) return;
        if (compact) _expandedGeometry = _token.GetBindValue(_geometry);
        _compact = compact;
        _token.SetGroupLayout(_host, View.BuildLayout(compact, _sample.ShowRemove));
        _token.SetBindValue(_geometry, new NuiRect(_expandedGeometry.X, _expandedGeometry.Y,
            View.WindowWidth, View.WindowHeight(compact)));
        UpdateView();
    }

    // No image-input or trait-action routing in Step 1; close through the chat command.
    public override void Close()
    {
        if (_closed) return;
        _closed = true;
        _token.Close();
    }
}
