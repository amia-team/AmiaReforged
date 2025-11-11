using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Time;

public interface IHarptosTimeService
{
    HarptosDateTime GetCurrentDateTime();
    HarptosDateTime Convert(DateTimeOffset realDateTime);
}

[ServiceBinding(typeof(IHarptosTimeService))]
public sealed class HarptosTimeService : IHarptosTimeService
{
    private const int ReferenceRealYear = 2025;
    private const int ReferenceGameYear = 1393;
    private static readonly int YearOffset = ReferenceGameYear - ReferenceRealYear;

    private readonly Func<DateTimeOffset> _nowProvider;

    public HarptosTimeService()
        : this(() => DateTimeOffset.UtcNow)
    {
    }

    internal HarptosTimeService(Func<DateTimeOffset> nowProvider)
    {
        _nowProvider = nowProvider;
    }

    public HarptosDateTime GetCurrentDateTime() => Convert(_nowProvider());

    public HarptosDateTime Convert(DateTimeOffset realDateTime)
    {
        int gameYear = realDateTime.Year + YearOffset;
        return HarptosCalendar.Convert(realDateTime, gameYear);
    }
}
