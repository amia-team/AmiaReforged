using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities.SummonOptions;

public sealed class SummonOptionsPresenter : ScryPresenter<SummonOptionsView>
{

    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    // Geometry bind to force window position
    private readonly NuiBind<NuiRect> _geometryBind = new("window_geometry");
    private static readonly NuiRect WindowPosition = new(360f, 100f, 320f, 350f);

    // PC Key variable names
    private const string EdkChoiceVar = "edk_choice";
    private const string EmdChoiceVar = "jj_MummyDust_Choice";
    private const string ScChoiceVar = "SummonType";
    private const string BgChoiceVar = "BgChoice";

    // Feat numbers for epic spells
    private const int FeatEpicMummyDust = 874;
    private const int FeatEpicDragonKnight = 875;

    public override SummonOptionsView View { get; }

    public override NuiWindowToken Token() => _token;

    public SummonOptionsPresenter(SummonOptionsView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), "Summon Options (WIP)")
        {
            Geometry = _geometryBind,
            Transparent = false,
            Resizable = true,
            Closable = true,
            Collapsed = false,
            Border = true
        };
    }

    public override void Create()
    {
        if (_window is null)
        {
            _player.SendServerMessage("The summon options window could not be created.", ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        // Force the window position using the bind
        Token().SetBindValue(_geometryBind, WindowPosition);

        // Check requirements and set enabled states
        SetEnabledStates();

        // Load current settings from PC Key
        LoadCurrentSettings();
    }

    private void SetEnabledStates()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null)
        {
            // Disable everything if no creature
            _token.SetBindValue(View.EdkEnabled, false);
            _token.SetBindValue(View.EmdEnabled, false);
            _token.SetBindValue(View.BgEnabled, false);
            return;
        }

        // Epic Dragon Knight requires feat 875
        bool hasEdk = NWScript.GetHasFeat(FeatEpicDragonKnight, creature) == NWScript.TRUE;
        _token.SetBindValue(View.EdkEnabled, hasEdk);

        // Epic Mummy Dust requires feat 874
        bool hasEmd = NWScript.GetHasFeat(FeatEpicMummyDust, creature) == NWScript.TRUE;
        _token.SetBindValue(View.EmdEnabled, hasEmd);

        // Blackguard Summon requires 5+ Blackguard levels
        int bgLevels = creature.GetClassInfo(ClassType.Blackguard)?.Level ?? 0;
        bool hasBg = bgLevels >= 5;
        _token.SetBindValue(View.BgEnabled, hasBg);
    }

    private void LoadCurrentSettings()
    {
        NwItem? pcKey = GetPcKey();
        if (pcKey == null)
        {
            // Set defaults if no PC Key
            _token.SetBindValue(View.EdkSelection, 12);
            _token.SetBindValue(View.EmdSelection, 1);
            _token.SetBindValue(View.SummonCreatureSelection, 0);
            _token.SetBindValue(View.BlackguardSelection, 1);

            _token.SetBindValue(View.EdkCurrentLabel, "Not Set");
            _token.SetBindValue(View.EmdCurrentLabel, "Not Set");
            _token.SetBindValue(View.ScCurrentLabel, "Not Set");
            _token.SetBindValue(View.BgCurrentLabel, "Not Set");
            return;
        }

        // Load EDK choice
        int edkChoice = pcKey.GetObjectVariable<LocalVariableInt>(EdkChoiceVar).Value;
        if (edkChoice == 0)
        {
            edkChoice = 1; // Default to Brass
        }
        _token.SetBindValue(View.EdkSelection, edkChoice);
        _token.SetBindValue(View.EdkCurrentLabel, SummonOptionsView.GetEdkLabel(edkChoice));

        // Load EMD choice
        int emdChoice = pcKey.GetObjectVariable<LocalVariableInt>(EmdChoiceVar).Value;
        _token.SetBindValue(View.EmdSelection, emdChoice);
        _token.SetBindValue(View.EmdCurrentLabel, SummonOptionsView.GetEmdLabel(emdChoice));

        // Load Summon Creature choice
        int scChoice = pcKey.GetObjectVariable<LocalVariableInt>(ScChoiceVar).Value;
        _token.SetBindValue(View.SummonCreatureSelection, scChoice);
        _token.SetBindValue(View.ScCurrentLabel, SummonOptionsView.GetScLabel(scChoice));

        // Load Blackguard choice
        int bgChoice = pcKey.GetObjectVariable<LocalVariableInt>(BgChoiceVar).Value;
        if (bgChoice == 0) bgChoice = 1; // Default to CE
        _token.SetBindValue(View.BlackguardSelection, bgChoice);
        _token.SetBindValue(View.BgCurrentLabel, SummonOptionsView.GetBgLabel(bgChoice));
    }

    private NwItem? GetPcKey()
    {
        return _player.LoginCreature?.Inventory.Items.FirstOrDefault(item => item.Tag == "ds_pckey");
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click) return;

        switch (ev.ElementId)
        {
            case "btn_set_edk":
                SetEdkChoice();
                break;
            case "btn_set_emd":
                SetEmdChoice();
                break;
            case "btn_set_sc":
                SetSummonCreatureChoice();
                break;
            case "btn_set_bg":
                SetBlackguardChoice();
                break;
        }
    }

    private void SetEdkChoice()
    {
        NwItem? pcKey = GetPcKey();
        if (pcKey == null)
        {
            _player.SendServerMessage("Error: Could not find your PC Key.", ColorConstants.Red);
            return;
        }

        int selection = _token.GetBindValue(View.EdkSelection);
        pcKey.GetObjectVariable<LocalVariableInt>(EdkChoiceVar).Value = selection;

        string label = SummonOptionsView.GetEdkLabel(selection);
        _token.SetBindValue(View.EdkCurrentLabel, label);

        _player.SendServerMessage($"Epic Dragon Knight summon set to: {label}", ColorConstants.Cyan);
    }

    private void SetEmdChoice()
    {
        NwItem? pcKey = GetPcKey();
        if (pcKey == null)
        {
            _player.SendServerMessage("Error: Could not find your PC Key.", ColorConstants.Red);
            return;
        }

        int selection = _token.GetBindValue(View.EmdSelection);
        pcKey.GetObjectVariable<LocalVariableInt>(EmdChoiceVar).Value = selection;

        string label = SummonOptionsView.GetEmdLabel(selection);
        _token.SetBindValue(View.EmdCurrentLabel, label);

        _player.SendServerMessage($"Epic Mummy Dust summon set to: {label}", ColorConstants.Cyan);
    }

    private void SetSummonCreatureChoice()
    {
        NwItem? pcKey = GetPcKey();
        if (pcKey == null)
        {
            _player.SendServerMessage("Error: Could not find your PC Key.", ColorConstants.Red);
            return;
        }

        int selection = _token.GetBindValue(View.SummonCreatureSelection);
        pcKey.GetObjectVariable<LocalVariableInt>(ScChoiceVar).Value = selection;

        string label = SummonOptionsView.GetScLabel(selection);
        _token.SetBindValue(View.ScCurrentLabel, label);

        _player.SendServerMessage($"Summon Creature I-IX set to: {label}", ColorConstants.Cyan);
    }

    private void SetBlackguardChoice()
    {
        NwItem? pcKey = GetPcKey();
        if (pcKey == null)
        {
            _player.SendServerMessage("Error: Could not find your PC Key.", ColorConstants.Red);
            return;
        }

        int selection = _token.GetBindValue(View.BlackguardSelection);
        pcKey.GetObjectVariable<LocalVariableInt>(BgChoiceVar).Value = selection;

        string label = SummonOptionsView.GetBgLabel(selection);
        _token.SetBindValue(View.BgCurrentLabel, label);

        _player.SendServerMessage($"Blackguard summon set to: {label}", ColorConstants.Cyan);
    }

    public override void UpdateView()
    {
        // No periodic updates needed
    }

    public override void Close()
    {
        // Don't call RaiseCloseEvent() here - it causes infinite recursion
        // The WindowDirector handles cleanup when CloseWindow() is called
        _token.Close();
    }
}
