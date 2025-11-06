using System;
using System.Globalization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Time;

public sealed record HarptosDateTime(
    int Year,
    HarptosMonth? Month,
    int? Day,
    HarptosFestival? Festival,
    TimeOnly TimeOfDay)
{
    public bool IsFestival => Festival.HasValue;

    public string ToDisplayString(CultureInfo? culture = null, bool useDiegeticTime = false, bool includeTime = true)
    {
        culture ??= CultureInfo.InvariantCulture;
        string timePortion = useDiegeticTime
            ? TimeOfDayNames.FormatTimeOfDay(TimeOfDay, includeTime: includeTime)
            : TimeOfDay.ToString("HH:mm", culture);

        string datePortion = IsFestival
            ? string.Format(culture, "{0}, {1} DR", Festival!.Value.GetDisplayName(), Year)
            : string.Format(
                culture,
                "{0} {1:00}, {2} DR",
                Month!.Value.GetPrimaryName(),
                Day!.Value,
                Year);

        return $"{datePortion} @ {timePortion}";
    }
}
