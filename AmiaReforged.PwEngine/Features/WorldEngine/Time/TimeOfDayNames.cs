using System;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Time;

/// <summary>
/// Provides diegetic (in-world) names for times of day based on Forgotten Realms conventions.
/// </summary>
public static class TimeOfDayNames
{
    /// <summary>
    /// Gets the diegetic name for a given time of day (e.g., "Dawn", "Highsun", "Moondark").
    /// </summary>
    /// <param name="timeOfDay">The time to get the name for.</param>
    /// <returns>A string representing the in-world name for that time of day.</returns>
    public static string GetTimeOfDayName(TimeOnly timeOfDay)
    {
        int hour = timeOfDay.Hour;
        
        return hour switch
        {
            >= 5 and < 6 => "Dawn",           // 5:00-5:59 - Around sunrise
            >= 6 and < 12 => "Morning",       // 6:00-11:59 - Between sunrise and noon
            12 => "Highsun",                  // 12:00-12:59 - Noon/twelve bells
            >= 13 and < 17 => "Afternoon",    // 13:00-16:59 - After noon
            >= 17 and < 18 => "Dusk",         // 17:00-17:59 - Before sunset
            >= 18 and < 19 => "Sunset",       // 18:00-18:59 - Around sunset
            >= 19 and < 24 => "Evening",      // 19:00-23:59 - After sunset
            0 => "Midnight",                  // 00:00-00:59 - Twelve bells
            >= 1 and < 3 => "Moondark",       // 01:00-02:59 - Darkest part of night/night's heart
            >= 3 and < 5 => "Night's end",    // 03:00-04:59 - Before sunrise
            _ => "Night"                      // Fallback
        };
    }
    
    /// <summary>
    /// Gets a formatted string combining the diegetic time name with the actual time.
    /// Example: "Morning (08:30)" or "Highsun (12:00)"
    /// </summary>
    /// <param name="timeOfDay">The time to format.</param>
    /// <param name="includeTime">Whether to include the numeric time in parentheses.</param>
    /// <returns>A formatted string with the time of day name.</returns>
    public static string FormatTimeOfDay(TimeOnly timeOfDay, bool includeTime = true)
    {
        string name = GetTimeOfDayName(timeOfDay);
        
        if (includeTime)
        {
            return $"{name} ({timeOfDay:HH:mm})";
        }
        
        return name;
    }
}
