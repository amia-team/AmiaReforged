using System.Collections.Immutable;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Time;

internal static class HarptosCalendar
{
    private const int DaysPerMonth = 30;
    private const int FestivalDayLength = 1;
    private const int NonLeapYearLength = 365;
    private const int LeapYearLength = 366;

    private static readonly ImmutableArray<HarptosCalendarSegment> NonLeapSegments =
        ImmutableArray.Create(
            HarptosCalendarSegment.CreateMonth(HarptosMonth.Hammer),
            HarptosCalendarSegment.CreateFestival(HarptosFestival.Midwinter),
            HarptosCalendarSegment.CreateMonth(HarptosMonth.Alturiak),
            HarptosCalendarSegment.CreateMonth(HarptosMonth.Ches),
            HarptosCalendarSegment.CreateMonth(HarptosMonth.Tarsakh),
            HarptosCalendarSegment.CreateFestival(HarptosFestival.Greengrass),
            HarptosCalendarSegment.CreateMonth(HarptosMonth.Mirtul),
            HarptosCalendarSegment.CreateMonth(HarptosMonth.Kythorn),
            HarptosCalendarSegment.CreateMonth(HarptosMonth.Flamerule),
            HarptosCalendarSegment.CreateFestival(HarptosFestival.Midsummer),
            HarptosCalendarSegment.CreateMonth(HarptosMonth.Eleasis),
            HarptosCalendarSegment.CreateMonth(HarptosMonth.Eleint),
            HarptosCalendarSegment.CreateFestival(HarptosFestival.Highharvestide),
            HarptosCalendarSegment.CreateMonth(HarptosMonth.Marpenoth),
            HarptosCalendarSegment.CreateMonth(HarptosMonth.Uktar),
            HarptosCalendarSegment.CreateFestival(HarptosFestival.FeastOfTheMoon),
            HarptosCalendarSegment.CreateMonth(HarptosMonth.Nightal));

    private static readonly ImmutableArray<HarptosCalendarSegment> LeapSegments = BuildLeapSegments();

    public static bool IsShieldmeetYear(int year) => year % 4 == 0;

    public static int GetYearLength(int year) => IsShieldmeetYear(year) ? LeapYearLength : NonLeapYearLength;

    public static HarptosDateTime Convert(DateTimeOffset realDateTime, int gameYear)
    {
        ImmutableArray<HarptosCalendarSegment> segments = IsShieldmeetYear(gameYear) ? LeapSegments : NonLeapSegments;
        int yearLength = IsShieldmeetYear(gameYear) ? LeapYearLength : NonLeapYearLength;

        int dayIndex = realDateTime.DayOfYear;
        if (dayIndex > yearLength)
        {
            dayIndex = ((dayIndex - 1) % yearLength) + 1;
        }

        int remaining = dayIndex;
        foreach (HarptosCalendarSegment segment in segments)
        {
            if (remaining <= segment.Length)
            {
                TimeOnly time = TimeOnly.FromTimeSpan(realDateTime.UtcDateTime.TimeOfDay);

                return segment.Type switch
                {
                    HarptosSegmentType.Month => new HarptosDateTime(
                        gameYear,
                        segment.Month,
                        remaining,
                        null,
                        time),
                    HarptosSegmentType.Festival => new HarptosDateTime(
                        gameYear,
                        null,
                        null,
                        segment.Festival,
                        time),
                    _ => throw new InvalidOperationException("Unsupported Harptos calendar segment type.")
                };
            }

            remaining -= segment.Length;
        }

        throw new InvalidOperationException("Unable to map day-of-year to Harptos calendar segment.");
    }

    private static ImmutableArray<HarptosCalendarSegment> BuildLeapSegments()
    {
        ImmutableArray<HarptosCalendarSegment>.Builder builder = ImmutableArray.CreateBuilder<HarptosCalendarSegment>(NonLeapSegments.Length + 1);
        foreach (HarptosCalendarSegment segment in NonLeapSegments)
        {
            builder.Add(segment);
            if (segment.Type == HarptosSegmentType.Festival && segment.Festival == HarptosFestival.Midsummer)
            {
                builder.Add(HarptosCalendarSegment.CreateFestival(HarptosFestival.Shieldmeet));
            }
        }

        return builder.MoveToImmutable();
    }

    private readonly record struct HarptosCalendarSegment(
        HarptosSegmentType Type,
        HarptosMonth? Month,
        HarptosFestival? Festival,
        int Length)
    {
        public static HarptosCalendarSegment CreateMonth(HarptosMonth month) =>
            new(HarptosSegmentType.Month, month, null, DaysPerMonth);

        public static HarptosCalendarSegment CreateFestival(HarptosFestival festival) =>
            new(HarptosSegmentType.Festival, null, festival, FestivalDayLength);
    }

    private enum HarptosSegmentType
    {
        Month,
        Festival
    }
}
