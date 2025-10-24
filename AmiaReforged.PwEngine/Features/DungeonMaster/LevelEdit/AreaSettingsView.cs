using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.LevelEdit;

public sealed class AreaSettingsView : ScryView<AreaSettingsPresenter>, IDmWindow
{
    public override AreaSettingsPresenter Presenter { get; protected set; }

    public string Title => "Area Settings";
    public bool ListInDmTools => false;
    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    // Reuse the binds from AreaEditorView that correspond to settings
    public readonly NuiBind<string> DayMusicStr = new("day_music_str");
    public readonly NuiBind<string> NightMusicStr = new("night_music_str");
    public readonly NuiBind<string> BattleMusicStr = new("battle_music_str");

    public readonly NuiBind<string> FogClipDistance = new("fog_clip_distance");

    public readonly NuiBind<string> DayFogR = new("day_fog_r");
    public readonly NuiBind<string> DayFogG = new("day_fog_g");
    public readonly NuiBind<string> DayFogB = new("day_fog_b");
    public readonly NuiBind<string> DayFogA = new("day_fog_a");

    public readonly NuiBind<string> DayDiffuseR = new("day_diffuse_r");
    public readonly NuiBind<string> DayDiffuseG = new("day_diffuse_g");
    public readonly NuiBind<string> DayDiffuseB = new("day_diffuse_b");
    public readonly NuiBind<string> DayDiffuseA = new("day_diffuse_a");

    public readonly NuiBind<string> DayFogDensity = new("day_fog_density");

    public readonly NuiBind<string> NightFogR = new("night_fog_r");
    public readonly NuiBind<string> NightFogG = new("night_fog_g");
    public readonly NuiBind<string> NightFogB = new("night_fog_b");
    public readonly NuiBind<string> NightFogA = new("night_fog_a");

    public readonly NuiBind<string> NightDiffuseR = new("night_diffuse_r");
    public readonly NuiBind<string> NightDiffuseG = new("night_diffuse_g");
    public readonly NuiBind<string> NightDiffuseB = new("night_diffuse_b");
    public readonly NuiBind<string> NightDiffuseA = new("night_diffuse_a");

    public readonly NuiBind<string> NightFogDensity = new("night_fog_color");

    public NuiButton SaveSettingsButton = null!;

    public AreaSettingsView(NwPlayer player)
    {
        Presenter = new AreaSettingsPresenter(this, player);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override NuiLayout RootLayout()
    {
        // A simplified layout containing the important settings fields and Save button
        return new NuiGroup
        {
            Element = new NuiColumn
            {
                Width = 300f,
                Children =
                [
                    new NuiLabel("Sound Settings"),
                    new NuiRow { Children = [ new NuiLabel("Day:"), new NuiTextEdit("0", DayMusicStr, 5, false) ]},
                    new NuiRow { Children = [ new NuiLabel("Night:"), new NuiTextEdit("0", NightMusicStr, 5, false) ]},
                    new NuiRow { Children = [ new NuiLabel("Battle:"), new NuiTextEdit("0", BattleMusicStr, 5, false) ]},
                    new NuiLabel("Fog Settings"),
                    new NuiRow { Children = [ new NuiLabel("Clip:"), new NuiTextEdit("0", FogClipDistance, 5, false) ]},
                    new NuiRow { Children = [ new NuiLabel("Day Density:"), new NuiTextEdit("0", DayFogDensity, 3, false) ]},
                    new NuiRow { Children = [ new NuiLabel("Night Density:"), new NuiTextEdit("0", NightFogDensity, 3, false) ]},
                    new NuiSpacer(),
                    new NuiButton("Save Settings") { Id = "btn_save_settings" }.Assign(out SaveSettingsButton)
                ]
            }
        };
    }
}

public sealed class AreaSettingsPresenter : ScryPresenter<AreaSettingsView>
{
    public override AreaSettingsView View { get; }

    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    [Inject] private Lazy<LevelEditorService>? LevelEditorService { get; init; }

    private LevelEditSession? _session;

    public AreaSettingsPresenter(AreaSettingsView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), "Area Settings") { Geometry = new NuiRect(0f, 100f, 320f, 280f) };
    }

    public override void Create()
    {
        if (_window is null) InitBefore();
        if (_window is null) return;

        _player.TryCreateNuiWindow(_window, out _token);

        // Acquire or create session for player's current area
        NwArea? area = _player.LoginCreature?.Area;
        if (area is null) return;

        _session = LevelEditorService?.Value.GetOrCreateSessionForArea(area);
        _session?.RegisterPresenter(View.Presenter);

        LoadFromSession();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType != NuiEventType.Click) return;

        if (obj.ElementId == View.SaveSettingsButton.Id)
        {
            SaveToArea();
        }
    }

    private void LoadFromSession()
    {
        if (_session is null) return;
        NwArea? area = _session.Area;
        if (area is null) return;

        Token().SetBindValue(View.DayMusicStr, area.MusicBackgroundDayTrack.ToString());
        Token().SetBindValue(View.NightMusicStr, area.MusicBackgroundNightTrack.ToString());
        Token().SetBindValue(View.BattleMusicStr, area.MusicBattleTrack.ToString());

        Token().SetBindValue(View.FogClipDistance, area.FogClipDistance.ToString());
        Token().SetBindValue(View.DayFogDensity, area.SunFogAmount.ToString());
        Token().SetBindValue(View.NightFogDensity, area.MoonFogAmount.ToString());
    }

    private void SaveToArea()
    {
        if (_session is null) return;
        NwArea area = _session.Area;

        string? day = Token().GetBindValue(View.DayMusicStr);
        string? night = Token().GetBindValue(View.NightMusicStr);
        string? battle = Token().GetBindValue(View.BattleMusicStr);

        if (int.TryParse(day, out int d)) area.MusicBackgroundDayTrack = d;
        if (int.TryParse(night, out int n)) area.MusicBackgroundNightTrack = n;
        if (int.TryParse(battle, out int b)) area.MusicBattleTrack = b;

        string? fog = Token().GetBindValue(View.FogClipDistance);
        if (int.TryParse(fog, out int f)) area.FogClipDistance = f;

        area.PlayBackgroundMusic();
        area.RecomputeStaticLighting();
    }

    public override void Close()
    {
        if (_session != null)
        {
            _session.UnregisterPresenter(View.Presenter);
            _session = null;
        }

        try
        {
            _token.Close();
        }
        catch
        {
            // ignore close failures
        }
    }
}
