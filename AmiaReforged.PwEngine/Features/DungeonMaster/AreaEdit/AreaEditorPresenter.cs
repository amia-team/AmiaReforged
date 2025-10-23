using System.Globalization;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.AreaEdit;

public sealed class AreaEditorPresenter : ScryPresenter<AreaEditorView>
{
    public override AreaEditorView View { get; }
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private readonly NwPlayer _player;

    private string _search = string.Empty;

    private NwArea? _selectedArea;

    private List<string> _visibleAreas = [];

    public AreaEditorPresenter(AreaEditorView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(0f, 100f, 400f, 600f)
        };
    }

    public override void Create()
    {
        if (_window == null)
            // Try to create the window if it doesn't exist.
            InitBefore();

        // If the window wasn't created, then tell the user we screwed up.
        if (_window == null)
        {
            _player.SendServerMessage(
                message: "The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);
        Token().SetBindWatch(View.SearchBind, true);

        UpdateAvailableList();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType == NuiEventType.Watch && obj.ElementId == View.SearchBind.Key)
        {
            _search = (Token().GetBindValue(View.SearchBind) ?? string.Empty).Trim();
            UpdateAvailableList();
            return;
        }

        if (obj.EventType == NuiEventType.Watch && obj.ElementId != View.SearchBind.Key)
        {
            _search = (Token().GetBindValue(View.SearchBind) ?? string.Empty).Trim();
            return;
        }

        switch (obj.EventType)
        {
            case NuiEventType.Click:
                HandleButtonClick(obj);
                break;
        }
    }

    private void LoadFromSelection()
    {
        if (_selectedArea is null) return;
        // Audio
        Token().SetBindValue(View.NightMusicStr, _selectedArea.MusicBackgroundNightTrack.ToString());
        Token().SetBindValue(View.DayMusicStr, _selectedArea.MusicBackgroundDayTrack.ToString());
        Token().SetBindValue(View.BattleMusicStr, _selectedArea.MusicBattleTrack.ToString());

        // Fog
        Token().SetBindValue(View.FogClipDistance, _selectedArea.FogClipDistance.ToString(CultureInfo.InvariantCulture));
        Token().SetBindValue(View.DayFogColor, _selectedArea.SunFogColor.ToString());
        Token().SetBindValue(View.DayDiffuse, _selectedArea.SunDiffuseColor.ToString());
        Token().SetBindValue(View.DayFogDensity, _selectedArea.SunFogAmount.ToString());

        Token().SetBindValue(View.NightFogColor, _selectedArea.MoonFogColor.ToString());
        Token().SetBindValue(View.NightDiffuse, _selectedArea.MoonDiffuseColor.ToString());
        Token().SetBindValue(View.NightFogDensity, _selectedArea.MoonFogAmount.ToString());

    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent evt)
    {
        if (evt.ElementId == View.SaveSettingsButton.Id)
        {
            SaveSettingsToArea();
            return;
        }

        if (evt.ElementId == View.PickCurrentAreaButton.Id)
        {
            NwCreature? c = _player.LoginCreature;
            if (c == null) return;

            if (c.Area == null) return;

            _selectedArea = c.Area;
            LoadFromSelection();
            return;
        }

        if (evt.ElementId == "btn_pick_row")
        {
            string areaShindig = _visibleAreas[evt.ArrayIndex].Split("-")[1];

            NwArea? area = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == areaShindig);
            if (area != null)
            {
                _selectedArea = area;
                LoadFromSelection();
            }
        }
    }

    private void SaveSettingsToArea()
    {
        if (_selectedArea is null) return;
        _selectedArea.StopBackgroundMusic();
        string? dayMusicStr = Token().GetBindValue(View.DayMusicStr);
        string? nightMusicStr = Token().GetBindValue(View.NightMusicStr);
        string? battleMusicStr = Token().GetBindValue(View.BattleMusicStr);
        string? fogClipStr = Token().GetBindValue(View.FogClipDistance);

        string? dayFogAmountStr = Token().GetBindValue(View.DayFogDensity);
        string? dayFogColorStr = Token().GetBindValue(View.DayFogColor);
        string? dayDiffuseStr = Token().GetBindValue(View.DayDiffuse);

        string? moonFogAmountStr = Token().GetBindValue(View.NightFogDensity);
        string? moonFogColorStr = Token().GetBindValue(View.NightFogColor);
        string? moonDiffuseStr = Token().GetBindValue(View.NightDiffuse);

        if (dayMusicStr is not null)
        {
            int dayMusic = int.Parse(dayMusicStr);
            _selectedArea.MusicBackgroundNightTrack = dayMusic;
        }

        if (nightMusicStr is not null)
        {
            int nightMusic = int.Parse(nightMusicStr);
            _selectedArea.MusicBackgroundNightTrack = nightMusic;
        }

        if (battleMusicStr is not null)
        {
            int battleMusic = int.Parse(battleMusicStr);
            _selectedArea.MusicBackgroundDayTrack = battleMusic;
        }

        if (fogClipStr is not null)
        {
            int fogClip = int.Parse(fogClipStr);
            _selectedArea.FogClipDistance = fogClip;
        }

        if (dayFogAmountStr is not null)
        {
            int dayFogAmount = int.Parse(dayFogAmountStr);
            _selectedArea.FogClipDistance = dayFogAmount;
        }

        if (dayFogColorStr is not null)
        {
            _selectedArea.SunFogColor = Color.FromRGBA(dayFogColorStr);
        }

        if (dayDiffuseStr is not null)
        {
            _selectedArea.MoonFogColor = Color.FromRGBA(dayDiffuseStr);
        }

        if (moonFogColorStr is not null)
        {
            _selectedArea.MoonFogColor = Color.FromRGBA(moonFogColorStr);
        }

        if (moonDiffuseStr is not null)
        {
            _selectedArea.MoonDiffuseColor = Color.FromRGBA(moonDiffuseStr);
        }
        if (moonFogAmountStr is not null)
        {
            int moonFogAmount = int.Parse(moonFogAmountStr);
            _selectedArea.MoonFogAmount = moonFogAmount;
        }

        _selectedArea?.PlayBackgroundMusic();
    }

    private void UpdateAvailableList()
    {
        _visibleAreas = GetVisible();
        Token().SetBindValue(View.AreaCount, _visibleAreas.Count);
        Token().SetBindValues(View.AreaNames, _visibleAreas);
    }

    private List<string> GetVisible()
    {
        if (string.IsNullOrWhiteSpace(_search))
            return NwModule.Instance.Areas.Select(a => $"{a.Name}-{a.ResRef}").ToList();
        string s = _search.ToLowerInvariant();

        return NwModule.Instance.Areas.Where(a =>
        {
            bool resRefHit = a.ResRef.Contains(s.ToLower(), StringComparison.InvariantCultureIgnoreCase);
            bool nameHit = a.Name.Contains(s.ToLower(), StringComparison.InvariantCultureIgnoreCase);
            return resRefHit || nameHit;
        }).Select(ar => $"{ar.Name} - ({ar.ResRef})").ToList();
    }


    public override void Close()
    {
    }
}
