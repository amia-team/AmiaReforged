using AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Nui;

/// <summary>Production trait selection using the approved graphical shell.</summary>
public sealed class TraitSelectionView : ScryView<TraitSelectionPresenter>, IToolWindow
{
    public const float WindowW = TraitSelectionGraphicalView.BaseWindowW;
    public const float WindowH = TraitSelectionGraphicalView.BaseWindowH;
    public const int EntriesPerPage = TraitSelectionGraphicalView.EntriesPerPage;
    public TraitSelectionGraphicalView Graphical { get; } = new();
    public NuiGroup LayoutHost { get; private set; } = null!;
    public NuiBind<string> DetailTitle => Graphical.DetailTitle;
    public NuiBind<string> DetailBody => Graphical.DetailBody;
    public NuiBind<bool> ShowSelectButton => Graphical.ShowSelectButton;
    public NuiBind<bool> ShowDeselectButton => Graphical.ShowDeselectButton;
    public NuiBind<string> BudgetLabel => Graphical.BudgetLabel;
    public NuiBind<string> PageInfo => Graphical.PageInfo;
    public NuiBind<bool> ShowPrevPage => Graphical.ShowPrevPage;
    public NuiBind<bool> ShowNextPage => Graphical.ShowNextPage;
    public List<NuiBind<string>> EntryNames => Graphical.EntryNames;
    public List<NuiBind<string>> EntrySubtitles => Graphical.EntrySubtitles;
    public List<NuiBind<bool>> EntryRowVisible => Graphical.EntryRowVisible;

    public TraitSelectionView(NwPlayer player)
    {
        Presenter = new TraitSelectionPresenter(this, player);
        AnvilCore.GetService<InjectionService>()!.Inject(Presenter);
    }

    public override TraitSelectionPresenter Presenter { get; protected set; }
    public string Id => "playertools.traitselection";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => true;
    public string Title => "Traits";
    public string CategoryTag => "Character";
    public IScryPresenter ForPlayer(NwPlayer player) => new TraitSelectionView(player).Presenter;
    public void SetScaleFactor(float scaleFactor) => Graphical.SetScaleFactor(scaleFactor);

    public override NuiLayout RootLayout() => LayoutHost = new NuiGroup
    {
        Id = "trait_host", Width = Graphical.WindowWidth, Margin = 0, Padding = 0,
        Border = false, Scrollbars = NuiScrollbars.None, Scissor = true, Element = Graphical.RootLayout()
    };
}
