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

    public readonly NuiBind<string> DayFogDensity = new("day_fog_density");

    public readonly NuiBind<string> NightFogR = new("night_fog_r");
    public readonly NuiBind<string> NightFogG = new("night_fog_g");
    public readonly NuiBind<string> NightFogB = new("night_fog_b");
    public readonly NuiBind<string> NightFogA = new("night_fog_a");

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
        // Helper to create a compact label
        NuiLabel L(string text) => new(text) { Height = 15f, Width = 90f, VerticalAlign = NuiVAlign.Middle };

        return new NuiGroup
        {
            Element = new NuiColumn
            {
                Width = 340f,
                Children =
                [
                    new NuiLabel("Sound Settings") { Height = 15f, VerticalAlign = NuiVAlign.Middle },
                    new NuiRow { Children = [ L("Day Music:"),   new NuiTextEdit("0", DayMusicStr, 5, false) { Width = 60f } ] },
                    new NuiRow { Children = [ L("Night Music:"), new NuiTextEdit("0", NightMusicStr, 5, false) { Width = 60f } ] },
                    new NuiRow { Children = [ L("Battle Music:"),new NuiTextEdit("0", BattleMusicStr, 5, false) { Width = 60f } ] },

                    new NuiSpacer { Height = 6f },

                    new NuiLabel("Fog Settings") { Height = 15f, VerticalAlign = NuiVAlign.Middle },
                    new NuiRow { Children = [ L("Clip Distance:"), new NuiTextEdit("0", FogClipDistance, 5, false) { Width = 60f } ] },

                    new NuiSpacer { Height = 3f },

                    // Day Fog
                    new NuiLabel("Day Fog") { Height = 15f, VerticalAlign = NuiVAlign.Middle },
                    new NuiRow { Children = [ L("Density:"), new NuiTextEdit("0", DayFogDensity, 3, false) { Width = 60f } ] },
                    new NuiRow { Children = [ L("Color R:"), new NuiTextEdit("0", DayFogR, 3, false) { Width = 60f } ] },
                    new NuiRow { Children = [ L("Color G:"), new NuiTextEdit("0", DayFogG, 3, false) { Width = 60f } ] },
                    new NuiRow { Children = [ L("Color B:"), new NuiTextEdit("0", DayFogB, 3, false) { Width = 60f } ] },
                    new NuiRow { Children = [ L("Color A:"), new NuiTextEdit("0", DayFogA, 3, false) { Width = 60f } ] },

                    new NuiSpacer { Height = 3f },

                    // Night Fog
                    new NuiLabel("Night Fog") { Height = 15f, VerticalAlign = NuiVAlign.Middle },
                    new NuiRow { Children = [ L("Density:"), new NuiTextEdit("0", NightFogDensity, 3, false) { Width = 60f } ] },
                    new NuiRow { Children = [ L("Color R:"), new NuiTextEdit("0", NightFogR, 3, false) { Width = 60f } ] },
                    new NuiRow { Children = [ L("Color G:"), new NuiTextEdit("0", NightFogG, 3, false) { Width = 60f } ] },
                    new NuiRow { Children = [ L("Color B:"), new NuiTextEdit("0", NightFogB, 3, false) { Width = 60f } ] },
                    new NuiRow { Children = [ L("Color A:"), new NuiTextEdit("0", NightFogA, 3, false) { Width = 60f } ] },

                    new NuiSpacer { Height = 8f },

                    new NuiButton("Save Settings") { Id = "btn_save_settings", Height = 28f }.Assign(out SaveSettingsButton)
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
        _window = new NuiWindow(View.RootLayout(), "Area Settings") { Geometry = new NuiRect(0f, 100f, 360f, 520f) };
    }

    public override void Create()
    {
        if (_window is null) InitBefore();
        if (_window is null) return;

        _player.TryCreateNuiWindow(_window, out _token);

        // Enable bind watch on all editable fields so we can sanitize input as the user types
        SetBindWatchAll(true);

        // Acquire or create session for player's current area
        NwArea? area = _player.LoginCreature?.Area;
        if (area is null) return;

        _session = LevelEditorService?.Value.GetOrCreateSessionForArea(area);
        _session?.RegisterPresenter(View.Presenter);

        LoadFromSession();
    }

    private void SetBindWatchAll(bool enable)
    {
        Token().SetBindWatch(View.DayMusicStr, enable);
        Token().SetBindWatch(View.NightMusicStr, enable);
        Token().SetBindWatch(View.BattleMusicStr, enable);
        Token().SetBindWatch(View.FogClipDistance, enable);

        Token().SetBindWatch(View.DayFogDensity, enable);
        Token().SetBindWatch(View.NightFogDensity, enable);

        Token().SetBindWatch(View.DayFogR, enable);
        Token().SetBindWatch(View.DayFogG, enable);
        Token().SetBindWatch(View.DayFogB, enable);
        Token().SetBindWatch(View.DayFogA, enable);

        Token().SetBindWatch(View.NightFogR, enable);
        Token().SetBindWatch(View.NightFogG, enable);
        Token().SetBindWatch(View.NightFogB, enable);
        Token().SetBindWatch(View.NightFogA, enable);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        switch (obj.EventType)
        {
            case NuiEventType.Click:
                if (obj.ElementId == View.SaveSettingsButton.Id)
                {
                    SaveToArea();
                }
                break;
            case NuiEventType.Watch:
                // Sanitize inputs live to keep values valid and numeric only
                SanitizeInputs();
                break;
        }
    }

    private static string DigitsOnly(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return new string(input.Where(char.IsDigit).ToArray());
    }

    private static string DigitsOnlyMax3(string? input)
    {
        string d = DigitsOnly(input);
        return d.Length > 3 ? d[..3] : d;
    }

    private void SanitizeInputs()
    {
        // Music and fog clip/density are ints; keep digits only
        Token().SetBindValue(View.DayMusicStr, DigitsOnly(Token().GetBindValue(View.DayMusicStr)));
        Token().SetBindValue(View.NightMusicStr, DigitsOnly(Token().GetBindValue(View.NightMusicStr)));
        Token().SetBindValue(View.BattleMusicStr, DigitsOnly(Token().GetBindValue(View.BattleMusicStr)));
        Token().SetBindValue(View.FogClipDistance, DigitsOnly(Token().GetBindValue(View.FogClipDistance)));

        Token().SetBindValue(View.DayFogDensity, DigitsOnly(Token().GetBindValue(View.DayFogDensity)));
        Token().SetBindValue(View.NightFogDensity, DigitsOnly(Token().GetBindValue(View.NightFogDensity)));

        // RGBA are bytes; keep up to 3 digits (0-255). We clamp during save.
        Token().SetBindValue(View.DayFogR, DigitsOnlyMax3(Token().GetBindValue(View.DayFogR)));
        Token().SetBindValue(View.DayFogG, DigitsOnlyMax3(Token().GetBindValue(View.DayFogG)));
        Token().SetBindValue(View.DayFogB, DigitsOnlyMax3(Token().GetBindValue(View.DayFogB)));
        Token().SetBindValue(View.DayFogA, DigitsOnlyMax3(Token().GetBindValue(View.DayFogA)));

        Token().SetBindValue(View.NightFogR, DigitsOnlyMax3(Token().GetBindValue(View.NightFogR)));
        Token().SetBindValue(View.NightFogG, DigitsOnlyMax3(Token().GetBindValue(View.NightFogG)));
        Token().SetBindValue(View.NightFogB, DigitsOnlyMax3(Token().GetBindValue(View.NightFogB)));
        Token().SetBindValue(View.NightFogA, DigitsOnlyMax3(Token().GetBindValue(View.NightFogA)));
    }

    private void LoadFromSession()
    {
        if (_session is null) return;
        NwArea area = _session.Area;

        Token().SetBindValue(View.DayMusicStr, area.MusicBackgroundDayTrack.ToString());
        Token().SetBindValue(View.NightMusicStr, area.MusicBackgroundNightTrack.ToString());
        Token().SetBindValue(View.BattleMusicStr, area.MusicBattleTrack.ToString());

        Token().SetBindValue(View.FogClipDistance, area.FogClipDistance.ToString());
        Token().SetBindValue(View.DayFogDensity, area.SunFogAmount.ToString());
        Token().SetBindValue(View.NightFogDensity, area.MoonFogAmount.ToString());

        // Load Day Fog RGBA
        Token().SetBindValue(View.DayFogR, area.SunFogColor.Red.ToString());
        Token().SetBindValue(View.DayFogG, area.SunFogColor.Green.ToString());
        Token().SetBindValue(View.DayFogB, area.SunFogColor.Blue.ToString());
        Token().SetBindValue(View.DayFogA, area.SunFogColor.Alpha.ToString());

        // Load Night Fog RGBA
        Token().SetBindValue(View.NightFogR, area.MoonFogColor.Red.ToString());
        Token().SetBindValue(View.NightFogG, area.MoonFogColor.Green.ToString());
        Token().SetBindValue(View.NightFogB, area.MoonFogColor.Blue.ToString());
        Token().SetBindValue(View.NightFogA, area.MoonFogColor.Alpha.ToString());
    }

    private static byte? ParseByteOrNull(string? s)
    {
        if (byte.TryParse(s, out byte b)) return b;
        return null;
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

        string? dayDensity = Token().GetBindValue(View.DayFogDensity);
        if (int.TryParse(dayDensity, out int dfd)) area.SetFogAmount(FogType.Sun, dfd);

        string? nightDensity = Token().GetBindValue(View.NightFogDensity);
        if (int.TryParse(nightDensity, out int nfd)) area.SetFogAmount(FogType.Moon, nfd);

        // Save Day Fog RGBA
        byte? dR = ParseByteOrNull(Token().GetBindValue(View.DayFogR));
        byte? dG = ParseByteOrNull(Token().GetBindValue(View.DayFogG));
        byte? dB = ParseByteOrNull(Token().GetBindValue(View.DayFogB));
        byte? dA = ParseByteOrNull(Token().GetBindValue(View.DayFogA));
        if (dR is not null && dG is not null && dB is not null && dA is not null)
        {
            int rgba = (dR.Value << 24) | (dG.Value << 16) | (dB.Value << 8) | dA.Value;
            area.SunFogColor = Color.FromRGBA(rgba);
        }

        // Save Night Fog RGBA
        byte? nR = ParseByteOrNull(Token().GetBindValue(View.NightFogR));
        byte? nG = ParseByteOrNull(Token().GetBindValue(View.NightFogG));
        byte? nB = ParseByteOrNull(Token().GetBindValue(View.NightFogB));
        byte? nA = ParseByteOrNull(Token().GetBindValue(View.NightFogA));
        if (nR is not null && nG is not null && nB is not null && nA is not null)
        {
            int rgba = (nR.Value << 24) | (nG.Value << 16) | (nB.Value << 8) | nA.Value;
            area.MoonFogColor = Color.FromRGBA(rgba);
        }

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
