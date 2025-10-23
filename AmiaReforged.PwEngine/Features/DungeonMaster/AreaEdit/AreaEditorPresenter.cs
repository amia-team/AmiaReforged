using System.Globalization;
using AmiaReforged.Core.Models.DmModels;
using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry.GenericWindows;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.IdentityModel.Tokens;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.AreaEdit;

public sealed class AreaEditorPresenter : ScryPresenter<AreaEditorView>
{
    private const string IsInstanceLocalInt = "is_instance";
    public override AreaEditorView View { get; }
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private readonly NwPlayer _player;

    private string _search = string.Empty;

    private NwArea? _selectedArea;

    private List<string> _visibleAreas = [];

    private List<DmArea> _savedAreas = [];

    [Inject] private Lazy<DmAreaService>? AreaService { get; init; }
    [Inject] private Lazy<WindowDirector>? WindowDirector { get; init; }

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

        bool canSave = NWScript.GetLocalInt(_selectedArea, IsInstanceLocalInt) != NWScript.TRUE;
        Token().SetBindValue(View.CanSaveArea, canSave);

        // Audio
        Token().SetBindValue(View.NightMusicStr, _selectedArea.MusicBackgroundNightTrack.ToString());
        Token().SetBindValue(View.DayMusicStr, _selectedArea.MusicBackgroundDayTrack.ToString());
        Token().SetBindValue(View.BattleMusicStr, _selectedArea.MusicBattleTrack.ToString());

        // Fog
        Token().SetBindValue(View.FogClipDistance,
            _selectedArea.FogClipDistance.ToString(CultureInfo.InvariantCulture));

        Token().SetBindValue(View.DayFogDensity, _selectedArea.SunFogAmount.ToString());
        Token().SetBindValue(View.NightFogDensity, _selectedArea.MoonFogAmount.ToString());

        Token().SetBindValue(View.DayFogR, _selectedArea.SunFogColor.Red.ToString());
        Token().SetBindValue(View.DayFogB, _selectedArea.SunFogColor.Blue.ToString());
        Token().SetBindValue(View.DayFogG, _selectedArea.SunFogColor.Green.ToString());
        Token().SetBindValue(View.DayFogA, _selectedArea.SunFogColor.Alpha.ToString());

        Token().SetBindValue(View.NightFogR, _selectedArea.MoonFogColor.Red.ToString());
        Token().SetBindValue(View.NightFogB, _selectedArea.MoonFogColor.Blue.ToString());
        Token().SetBindValue(View.NightFogG, _selectedArea.MoonFogColor.Green.ToString());
        Token().SetBindValue(View.NightFogA, _selectedArea.MoonFogColor.Alpha.ToString());

        Token().SetBindValue(View.DayDiffuseR, _selectedArea.SunDiffuseColor.Red.ToString());
        Token().SetBindValue(View.DayDiffuseB, _selectedArea.SunDiffuseColor.Blue.ToString());
        Token().SetBindValue(View.DayDiffuseG, _selectedArea.SunDiffuseColor.Green.ToString());
        Token().SetBindValue(View.DayDiffuseA, _selectedArea.SunDiffuseColor.Alpha.ToString());

        Token().SetBindValue(View.NightDiffuseR, _selectedArea.MoonDiffuseColor.Red.ToString());
        Token().SetBindValue(View.NightDiffuseB, _selectedArea.MoonDiffuseColor.Blue.ToString());
        Token().SetBindValue(View.NightDiffuseG, _selectedArea.MoonDiffuseColor.Green.ToString());
        Token().SetBindValue(View.NightDiffuseA, _selectedArea.MoonDiffuseColor.Alpha.ToString());
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent evt)
    {
        if (AreaService is null) return;

        if (evt.ElementId == View.SaveNewInstanceButton.Id)
        {
            if (_selectedArea is null) return;

            SaveInstance();

            return;
        }

        if (evt.ElementId == View.ReloadCurrentAreaButton.Id)
        {
            HandleReload();
            return;
        }

        if (evt.ElementId == View.PickCurrentAreaButton.Id)
        {
            HandlePickCurrent();
            UpdateInstanceList();

            return;
        }

        if (evt.ElementId == View.SaveSettingsButton.Id)
        {
            SaveSettingsToArea();
            return;
        }


        if (evt.ElementId == "btn_pick_row")
        {
            HandlePickNew(evt);
            UpdateInstanceList();
            return;
        }


        if (evt.ElementId == "btn_delete_var")
        {
            HandleDelete(evt);
            UpdateInstanceList();
        }

        if (evt.ElementId == "btn_load_var")
        {
            HandleClone(evt);
            UpdateInstanceList();
        }
    }

    private void HandleClone(ModuleEvents.OnNuiEvent evt)
    {
        DmArea cloneMe = _savedAreas[evt.ArrayIndex];

        NwArea? area = NwArea.Deserialize(cloneMe.SerializedARE, cloneMe.SerializedGIT, $"{_player.CDKey}_{cloneMe.OriginalResRef}_{cloneMe.Id}", cloneMe.NewName);

        if (area is null)
        {
            _player.SendServerMessage("Failed to make the area.");
            return;
        }

        NWScript.SetLocalInt(area, IsInstanceLocalInt, NWScript.TRUE);
        _player.SendServerMessage($"{area.Name} created");
    }

    private void HandleDelete(ModuleEvents.OnNuiEvent evt)
    {
        if (WindowDirector is null) return;

        DmArea area = _savedAreas[evt.ArrayIndex];

        WindowDirector.Value.OpenPopupWithReaction(_player,
            "Are you sure you want to delete this Instance?",
            "This action is permanent!",
            () =>
            {
                AreaService!.Value.Delete(area);
                UpdateInstanceList();
            },
            false,
            Token()
        );
    }

    private void SaveInstance()
    {
        string? newInstanceName = Token().GetBindValue(View.NewAreaName);

        if (newInstanceName.IsNullOrEmpty())
        {
            _player.SendServerMessage("Name Input Cannot Be Empty");
            return;
        }

        DmArea? existing = AreaService.Value.InstanceFromKey(_player.CDKey, _selectedArea.ResRef, newInstanceName!);

        if (existing is null)
        {
            byte[]? serializedAre = _selectedArea.SerializeARE();
            if (serializedAre is null)
            {
                _player.SendServerMessage("Failed to serialize ARE");
                return;
            }

            byte[]? serializedGit = _selectedArea.SerializeGIT();
            if (serializedGit is null)
            {
                _player.SendServerMessage("Failed to serialize GIT");
                return;
            }

            DmArea newInstance = new DmArea
            {
                CdKey = _player.CDKey,
                OriginalResRef = _selectedArea.ResRef,
                NewName = newInstanceName!,
                SerializedARE = serializedAre,
                SerializedGIT = serializedGit
            };

            AreaService.Value.SaveNew(newInstance);
        }
        else
        {
            byte[]? serializedAre = _selectedArea.SerializeARE();
            if (serializedAre is null)
            {
                _player.SendServerMessage("Failed to serialize ARE");
                return;
            }

            byte[]? serializedGit = _selectedArea.SerializeGIT();
            if (serializedGit is null)
            {
                _player.SendServerMessage("Failed to serialize GIT");
                return;
            }

            existing.SerializedGIT = serializedGit;
            existing.SerializedGIT = serializedAre;

            AreaService.Value.SaveArea(existing);
        }

        UpdateInstanceList();
    }

    private void UpdateInstanceList()
    {
        if (AreaService is null) return;
        if (_selectedArea is null) return;

        _savedAreas = AreaService.Value.AllFromResRef(_player.CDKey, _selectedArea.ResRef);

        List<string> names = _savedAreas.Select(a => a.NewName).ToList();

        Token().SetBindValues(View.SavedVariantNames, names);
        Token().SetBindValue(View.SavedVariantCounts, names.Count);
    }

    private void HandleReload()
    {
        if (_selectedArea is null) return;
        if (_player.LoginCreature is null) return;
        if (_player.LoginCreature.Location is null) return;

        List<(NwCreature c, Location l)> allCurrent = [];

        allCurrent.Add((_player.LoginCreature, _player.LoginCreature.Location));
        _player.LoginCreature.Location = NwModule.Instance.StartingLocation;

        foreach (NwCreature creature in _selectedArea.FindObjectsOfTypeInArea<NwCreature>())
        {
            if (creature is { IsLoginPlayerCharacter: false }) continue;

            if (creature.Location == null) continue;

            allCurrent.Add((creature, creature.Location));

            _player.SendServerMessage($"Jumping {creature.Name}");
            creature.ActionJumpToLocation(NwModule.Instance.StartingLocation);
        }

        NWScript.DelayCommand(5.0f, () =>
        {
            foreach ((NwCreature c, Location l) cl in allCurrent)
            {
                cl.c.Location = cl.l;
            }
        });
    }

    private void HandlePickCurrent()
    {
        NwCreature? c = _player.LoginCreature;
        if (c == null) return;

        if (c.Area == null) return;

        _selectedArea = c.Area;
        LoadFromSelection();
    }

    private void HandlePickNew(ModuleEvents.OnNuiEvent evt)
    {
        string areaShindig = _visibleAreas[evt.ArrayIndex].Split("|")[1];

        NwArea? area = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == areaShindig);
        if (area == null) return;

        _selectedArea = area;
        LoadFromSelection();
    }

    private void SaveSettingsToArea()
    {
        if (_selectedArea is null) return;
        _selectedArea.StopBackgroundMusic();
        string? dayMusicStr = Token().GetBindValue(View.DayMusicStr);
        string? nightMusicStr = Token().GetBindValue(View.NightMusicStr);
        string? battleMusicStr = Token().GetBindValue(View.BattleMusicStr);
        string? fogClipStr = Token().GetBindValue(View.FogClipDistance);
        string? dayFogDensityStr = Token().GetBindValue(View.DayFogDensity);
        string? nightFogDensityStr = Token().GetBindValue(View.NightFogDensity);
        string? dayFogRStr = Token().GetBindValue(View.DayFogR);
        string? dayFogGStr = Token().GetBindValue(View.DayFogG);
        string? dayFogBStr = Token().GetBindValue(View.DayFogB);
        string? dayFogAStr = Token().GetBindValue(View.DayFogA);
        string? nightFogRStr = Token().GetBindValue(View.NightFogR);
        string? nightFogGStr = Token().GetBindValue(View.NightFogG);
        string? nightFogBStr = Token().GetBindValue(View.NightFogB);
        string? nightFogAStr = Token().GetBindValue(View.NightFogA);
        string? dayDiffuseRStr = Token().GetBindValue(View.DayDiffuseR);
        string? dayDiffuseGStr = Token().GetBindValue(View.DayDiffuseG);
        string? dayDiffuseBStr = Token().GetBindValue(View.DayDiffuseB);
        string? dayDiffuseAStr = Token().GetBindValue(View.DayDiffuseA);
        string? nightDiffuseRStr = Token().GetBindValue(View.NightDiffuseR);
        string? nightDiffuseGStr = Token().GetBindValue(View.NightDiffuseG);
        string? nightDiffuseBStr = Token().GetBindValue(View.NightDiffuseB);
        string? nightDiffuseAStr = Token().GetBindValue(View.NightDiffuseA);

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

        if (dayFogDensityStr is not null)
        {
            int dayFogAmount = int.Parse(dayFogDensityStr);

            _selectedArea.SetFogAmount(FogType.Sun, dayFogAmount);
        }

        if (nightFogDensityStr is not null)
        {
            int nightFogAmount = int.Parse(nightFogDensityStr);

            _selectedArea.SetFogAmount(FogType.Moon, nightFogAmount);
        }

        if (dayFogRStr is not null && dayFogGStr is not null && dayFogBStr is not null && dayFogAStr is not null)
        {
            byte r = byte.Parse(dayFogRStr);
            byte g = byte.Parse(dayFogGStr);
            byte b = byte.Parse(dayFogBStr);
            byte a = byte.Parse(dayFogAStr);

            int rgba = (r << 24) | (g << 16) | (b << 8) | a;

            Color newColor = Color.FromRGBA(rgba);

            _selectedArea.SunFogColor = newColor;
        }

        if (dayDiffuseRStr is not null && dayDiffuseBStr is not null && dayDiffuseGStr is not null &&
            dayDiffuseAStr is not null)
        {
            byte r = byte.Parse(dayDiffuseRStr);
            byte g = byte.Parse(dayDiffuseBStr);
            byte b = byte.Parse(dayDiffuseGStr);
            byte a = byte.Parse(dayDiffuseAStr);

            int rgba = (r << 24) | (g << 16) | (b << 8) | a;

            Color newColor = Color.FromRGBA(rgba);
            _selectedArea.SunFogColor = newColor;
        }

        if (nightFogRStr is not null && nightFogGStr is not null && nightFogBStr is not null &&
            nightFogAStr is not null)
        {
            byte r = byte.Parse(nightFogRStr);
            byte g = byte.Parse(nightFogGStr);
            byte b = byte.Parse(nightFogBStr);
            byte a = byte.Parse(nightFogAStr);

            int rgba = (r << 24) | (g << 16) | (b << 8) | a;

            Color newColor = Color.FromRGBA(rgba);

            _selectedArea.MoonFogColor = newColor;
        }

        if (nightDiffuseRStr is not null && nightDiffuseBStr is not null && nightDiffuseGStr is not null &&
            nightDiffuseAStr is not null)
        {
            byte r = byte.Parse(nightDiffuseRStr);
            byte g = byte.Parse(nightDiffuseBStr);
            byte b = byte.Parse(nightDiffuseGStr);
            byte a = byte.Parse(nightDiffuseAStr);

            int rgba = (r << 24) | (g << 16) | (b << 8) | a;
            Color newColor = Color.FromRGBA(rgba);
            _selectedArea.MoonDiffuseColor = newColor;
        }

        _selectedArea?.PlayBackgroundMusic();
        _selectedArea?.RecomputeStaticLighting();
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
            return NwModule.Instance.Areas.Select(a => $"{a.Name}|{a.ResRef}").ToList();
        string s = _search.ToLowerInvariant();

        return NwModule.Instance.Areas.Where(a =>
        {
            bool resRefHit = a.ResRef.Contains(s.ToLower(), StringComparison.InvariantCultureIgnoreCase);
            bool nameHit = a.Name.Contains(s.ToLower(), StringComparison.InvariantCultureIgnoreCase);
            return resRefHit || nameHit;
        }).Select(ar => $"{ar.Name}|{ar.ResRef}").ToList();
    }


    public override void Close()
    {
    }
}
