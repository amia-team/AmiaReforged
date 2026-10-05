using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Nui;

/// <summary>Production dialogue view using the approved graphical shell and its live binds.</summary>
public sealed class ConversationView : ScryView<ConversationPresenter>
{
    public const float BaseWindowW = ConversationGraphicalView.BaseWindowW;
    public const float BaseWindowH = ConversationGraphicalView.BaseWindowH;
    public const float BaseWindowX = 60f;
    public const float BaseWindowY = 80f;
    public const int MaxVisibleChoices = ConversationGraphicalView.MaxVisibleChoices;

    public ConversationGraphicalView Graphical { get; } = new();
    public NuiBind<string> SpeakerName => Graphical.SpeakerName;
    public NuiBind<string> NpcPortrait => Graphical.NpcPortrait;
    public NuiBind<string> NpcText => Graphical.NpcText;
    public NuiBind<string> TextPageInfo => Graphical.TextPageInfo;
    public NuiBind<bool> ShowPrevTextPage => Graphical.ShowPrevTextPage;
    public NuiBind<bool> ShowNextTextPage => Graphical.ShowNextTextPage;
    public NuiBind<bool> ShowTextPagination => Graphical.ShowTextPagination;
    public List<NuiBind<string>> ChoiceTexts => Graphical.ChoiceTexts;
    public List<NuiBind<bool>> ChoiceVisible => Graphical.ChoiceVisible;
    public NuiBind<bool> ShowMoreButton => Graphical.ShowMoreButton;
    public NuiBind<string> MoreButtonText => Graphical.MoreButtonText;
    public NuiBind<string> GoodbyeText => Graphical.GoodbyeText;

    public ConversationView(NwPlayer player, AmiaDialogueService amiaDialogueService)
    {
        Presenter = new ConversationPresenter(this, player, amiaDialogueService);
        AnvilCore.GetService<InjectionService>()!.Inject(Presenter);
    }

    public override ConversationPresenter Presenter { get; protected set; }
    public void SetScaleFactor(float scaleFactor) => Graphical.SetScaleFactor(scaleFactor);
    public override NuiLayout RootLayout() => Graphical.RootLayout();
}
