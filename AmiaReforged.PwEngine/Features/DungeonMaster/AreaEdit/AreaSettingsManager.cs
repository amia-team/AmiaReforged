using System.Globalization;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.AreaEdit;

/// <summary>
/// Handles loading and saving area settings
/// </summary>
public sealed class AreaSettingsManager
{
    private readonly NuiWindowToken _token;
    private readonly AreaEditorView _view;

    public AreaSettingsManager(NuiWindowToken token, AreaEditorView view)
    {
        _token = token;
        _view = view;
    }

    public void LoadToUI(NwArea area)
    {
        var settings = AreaSettings.FromArea(area);

        // Audio
        _token.SetBindValue(_view.DayMusicStr, settings.DayMusic.ToString());
        _token.SetBindValue(_view.NightMusicStr, settings.NightMusic.ToString());
        _token.SetBindValue(_view.BattleMusicStr, settings.BattleMusic.ToString());

        // Fog
        _token.SetBindValue(_view.FogClipDistance, settings.FogClipDistance.ToString(CultureInfo.InvariantCulture));
        _token.SetBindValue(_view.DayFogDensity, settings.DayFogDensity.ToString());
        _token.SetBindValue(_view.NightFogDensity, settings.NightFogDensity.ToString());

        SetColorBinds(settings.DayFogColor, _view.DayFogR, _view.DayFogG, _view.DayFogB, _view.DayFogA);
        SetColorBinds(settings.NightFogColor, _view.NightFogR, _view.NightFogG, _view.NightFogB, _view.NightFogA);
        SetColorBinds(settings.DayDiffuseColor, _view.DayDiffuseR, _view.DayDiffuseG, _view.DayDiffuseB, _view.DayDiffuseA);
        SetColorBinds(settings.NightDiffuseColor, _view.NightDiffuseR, _view.NightDiffuseG, _view.NightDiffuseB, _view.NightDiffuseA);
    }

    public AreaSettings LoadFromUI()
    {
        return new AreaSettings
        {
            DayMusic = ParseInt(_view.DayMusicStr),
            NightMusic = ParseInt(_view.NightMusicStr),
            BattleMusic = ParseInt(_view.BattleMusicStr),
            FogClipDistance = ParseInt(_view.FogClipDistance),
            DayFogDensity = ParseInt(_view.DayFogDensity),
            NightFogDensity = ParseInt(_view.NightFogDensity),
            DayFogColor = ParseColor(_view.DayFogR, _view.DayFogG, _view.DayFogB, _view.DayFogA),
            NightFogColor = ParseColor(_view.NightFogR, _view.NightFogG, _view.NightFogB, _view.NightFogA),
            DayDiffuseColor = ParseColor(_view.DayDiffuseR, _view.DayDiffuseG, _view.DayDiffuseB, _view.DayDiffuseA),
            NightDiffuseColor = ParseColor(_view.NightDiffuseR, _view.NightDiffuseG, _view.NightDiffuseB, _view.NightDiffuseA)
        };
    }

    private void SetColorBinds(Color color, NuiBind<string> r, NuiBind<string> g, NuiBind<string> b, NuiBind<string> a)
    {
        _token.SetBindValue(r, color.Red.ToString());
        _token.SetBindValue(g, color.Green.ToString());
        _token.SetBindValue(b, color.Blue.ToString());
        _token.SetBindValue(a, color.Alpha.ToString());
    }

    private Color ParseColor(NuiBind<string> r, NuiBind<string> g, NuiBind<string> b, NuiBind<string> a)
    {
        byte red = ParseByte(r);
        byte green = ParseByte(g);
        byte blue = ParseByte(b);
        byte alpha = ParseByte(a);

        int rgba = (red << 24) | (green << 16) | (blue << 8) | alpha;
        return Color.FromRGBA(rgba);
    }

    private int ParseInt(NuiBind<string> bind)
    {
        string? value = _token.GetBindValue(bind);
        return value is not null ? int.Parse(value) : 0;
    }

    private byte ParseByte(NuiBind<string> bind)
    {
        string? value = _token.GetBindValue(bind);
        return value is not null ? byte.Parse(value) : (byte)0;
    }
}
