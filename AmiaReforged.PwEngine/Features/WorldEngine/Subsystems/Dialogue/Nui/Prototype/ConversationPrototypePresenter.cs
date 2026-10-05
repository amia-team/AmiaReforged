using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Nui.Prototype;

/// <summary>Visual-only dialogue shell; never creates or advances a dialogue session.</summary>
public sealed class ConversationPrototypePresenter(NwPlayer player, DevicePropertyService device, string scenario, bool corner,
    int? choiceCount = null)
    : ScryPresenter<ConversationGraphicalView>
{
    private readonly NuiBind<NuiRect> _geometry = new("conv_preview_geometry");
    private NuiWindow _window = null!;
    private NuiWindowToken _token;
    private float _scaleFactor;
    private bool _closed;
    public override ConversationGraphicalView View { get; } = new();
    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _scaleFactor = device.GetGuiScale(player) / 100f;
        if (_scaleFactor <= 0) _scaleFactor = 1;
        View.SetScaleFactor(_scaleFactor);
        _window = new NuiWindow(View.RootLayout(), null!)
        {
            Geometry = _geometry, Transparent = true, Border = false,
            Resizable = false, Closable = false, Collapsed = false
        };
    }

    public override void Create()
    {
        if (!player.TryCreateNuiWindow(_window, out _token))
            throw new InvalidOperationException("The client could not create the dialogue preview window.");

        _token.SetBindValue(_geometry, new NuiRect(corner ? 60 / _scaleFactor : -1,
            corner ? 80 / _scaleFactor : -1, View.WindowWidth, View.WindowHeight));
        UpdateView();
        if (scenario == "speaker") _ = ChangeSpeakerAsync();
        player.SendServerMessage($"Dialogue preview / {scenario}: viewport {device.GetGuiWidth(player)}x{device.GetGuiHeight(player)}, " +
            $"GUI {device.GetGuiScale(player)}%, physical shell {ConversationGraphicalView.BaseWindowW}x{ConversationGraphicalView.BaseWindowH}. " +
            "Images are visual-only. Repeat ./dialogueui to close, or choose another scenario.", ColorConstants.Cyan);
    }

    public override void UpdateView()
    {
        _token.SetBindValue(View.ControlsEnabled, true);
        _token.SetBindValue(View.ChoicesEnabled, true);
        bool longContent = scenario == "long";
        bool single = scenario is "single" or "empty" or "choices";
        int choices = choiceCount ?? (scenario == "empty" ? 0 : scenario == "single" ? 1 : 5);
        NwCreature? sampleSpeaker = scenario == "speaker"
            ? player.LoginCreature?.Area?.Objects.OfType<NwCreature>().FirstOrDefault(creature => creature.IsValid && !creature.IsPlayerControlled(out _))
            : null;
        string name = scenario == "speaker" ? sampleSpeaker?.Name ?? "Guildhouse Guard Nefzen" : longContent
            ? "Guildhouse Guard Nefzen, Keeper of the Northern Gate and the Old Watchtower" : "Guildhouse Guard Nefzen";
        string body = single ? "This is a single text page. The pagination strip stays reserved, and hidden response slots must not move the footer." :
            "Duis commodo felis vitae mauris interdum condimentum. Quisque in pretium quam, posuere bibendum sem. " +
            "Vivamus eu nulla cursus, congue velit vel, pharetra est.\n\nPraesent feugiat ante vitae luctus lobortis. " +
            "Maecenas ante ante, cursus imperdiet lorem faucibus, sagittis ullamcorper mi.\n\nInteger nec ligula id ipsum condimentum.";
        if (longContent)
            body = string.Join("\n\n", Enumerable.Range(1, 12).Select(i => $"Passage {i}. {body}")) +
                   "\n\nEND OF LONG SAMPLE — this final line must be reachable using the text scrollbar.";

        _token.SetBindValue(View.SpeakerName, name);
        string portrait = (sampleSpeaker ?? player.LoginCreature)?.PortraitResRef ?? "";
        _token.SetBindValue(View.NpcPortrait, string.IsNullOrEmpty(portrait) ? "" : portrait + "h");
        _token.SetBindValue(View.NpcText, body);
        _token.SetBindValue(View.TextPageInfo, single ? "" : scenario == "overflow" ? "1/3" : scenario == "last" ? "3/3" : "4/8");
        _token.SetBindValue(View.ShowTextPagination, !single);
        bool previous = !single && scenario != "overflow";
        bool next = !single && scenario != "last";
        _token.SetBindValue(View.ShowPrevTextPage, previous);
        _token.SetBindValue(View.ShowNextTextPage, next);
        _token.SetBindValue(View.PreviousColor, previous ? ConversationGraphicalView.Gold : ConversationGraphicalView.Muted);
        _token.SetBindValue(View.NextColor, next ? ConversationGraphicalView.Gold : ConversationGraphicalView.Muted);
        _token.SetBindValue(View.ShowMoreButton, !single);
        _token.SetBindValue(View.MoreButtonText, scenario == "overflow" ? "More (2/3)" : scenario == "last" ? "More (3/3)" : "More (1/2)");
        _token.SetBindValue(View.GoodbyeText, "Goodbye");
        string[] responses = ["Goodbye", "Tell me about the guildhouse.", "Who keeps watch at the northern gate?", "I have another question.", "Continue"];
        for (int i = 0; i < ConversationGraphicalView.MaxVisibleChoices; i++)
        {
            _token.SetBindValue(View.ChoiceVisible[i], i < choices);
            _token.SetBindValue(View.ChoiceTexts[i], i >= choices ? "" : longContent && i == 1
                ? "I would like to ask about the history of the guildhouse, its founding members, and the guards who watch the northern gate during the longest nights of winter."
                : responses[i]);
        }
    }

    private async Task ChangeSpeakerAsync()
    {
        await NwTask.Delay(TimeSpan.FromSeconds(3));
        await NwTask.SwitchToMainThread();
        if (_closed || player.LoginCreature is not { IsValid: true } speaker) return;
        _token.SetBindValue(View.SpeakerName, speaker.Name);
        string portrait = speaker.PortraitResRef;
        _token.SetBindValue(View.NpcPortrait, string.IsNullOrEmpty(portrait) ? "" : portrait + "h");
        player.SendServerMessage("Dialogue preview: speaker binds changed in the same window. Check both headings and portrait bounds.", ColorConstants.Cyan);
    }

    // No image press/release or dialogue routing in Step 1. Close through the chat command.
    public override void Close()
    {
        if (_closed) return;
        _closed = true;
        _token.Close();
    }
}
