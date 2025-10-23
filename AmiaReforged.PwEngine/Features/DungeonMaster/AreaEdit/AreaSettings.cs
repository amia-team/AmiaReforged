using Anvil.API;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.AreaEdit;

/// <summary>
/// Represents all editable settings for an area
/// </summary>
public sealed class AreaSettings
{
    // Audio
    public int DayMusic { get; set; }
    public int NightMusic { get; set; }
    public int BattleMusic { get; set; }

    // Fog
    public float FogClipDistance { get; set; }
    public int DayFogDensity { get; set; }
    public int NightFogDensity { get; set; }
    public Color DayFogColor { get; set; }
    public Color NightFogColor { get; set; }

    // Lighting
    public Color DayDiffuseColor { get; set; }
    public Color NightDiffuseColor { get; set; }

    public static AreaSettings FromArea(NwArea area)
    {
        return new AreaSettings
        {
            DayMusic = area.MusicBackgroundDayTrack,
            NightMusic = area.MusicBackgroundNightTrack,
            BattleMusic = area.MusicBattleTrack,
            FogClipDistance = area.FogClipDistance,
            DayFogDensity = area.SunFogAmount,
            NightFogDensity = area.MoonFogAmount,
            DayFogColor = area.SunFogColor,
            NightFogColor = area.MoonFogColor,
            DayDiffuseColor = area.SunDiffuseColor,
            NightDiffuseColor = area.MoonDiffuseColor
        };
    }

    public void ApplyToArea(NwArea area)
    {
        area.StopBackgroundMusic();

        // Audio
        area.MusicBackgroundDayTrack = DayMusic;
        area.MusicBackgroundNightTrack = NightMusic;
        area.MusicBattleTrack = BattleMusic;

        // Fog
        area.FogClipDistance = FogClipDistance;
        area.SetFogAmount(FogType.Sun, DayFogDensity);
        area.SetFogAmount(FogType.Moon, NightFogDensity);
        area.SunFogColor = DayFogColor;
        area.MoonFogColor = NightFogColor;

        // Lighting
        area.SunDiffuseColor = DayDiffuseColor;
        area.MoonDiffuseColor = NightDiffuseColor;

        area.PlayBackgroundMusic();
        area.RecomputeStaticLighting();
    }
}
