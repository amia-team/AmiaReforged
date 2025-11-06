using System.Collections.Generic;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Time;

public enum HarptosMonth
{
    Hammer = 1,
    Alturiak,
    Ches,
    Tarsakh,
    Mirtul,
    Kythorn,
    Flamerule,
    Eleasis,
    Eleint,
    Marpenoth,
    Uktar,
    Nightal
}

public static class HarptosMonthExtensions
{
    private static readonly IReadOnlyDictionary<HarptosMonth, (string Primary, string? Alias)> DisplayNames =
        new Dictionary<HarptosMonth, (string Primary, string? Alias)>
        {
            { HarptosMonth.Hammer, ("Hammer", "Deepwinter") },
            { HarptosMonth.Alturiak, ("Alturiak", "The Claw of Winter") },
            { HarptosMonth.Ches, ("Ches", "The Claw of Sunsets") },
            { HarptosMonth.Tarsakh, ("Tarsakh", "The Claw of Storms") },
            { HarptosMonth.Mirtul, ("Mirtul", "The Melting") },
            { HarptosMonth.Kythorn, ("Kythorn", "The Time of Flowers") },
            { HarptosMonth.Flamerule, ("Flamerule", "Summertide") },
            { HarptosMonth.Eleasis, ("Eleasis", "Highsun") },
            { HarptosMonth.Eleint, ("Eleint", "The Fading") },
            { HarptosMonth.Marpenoth, ("Marpenoth", "Leaffall") },
            { HarptosMonth.Uktar, ("Uktar", "The Rotting") },
            { HarptosMonth.Nightal, ("Nightal", "The Drawing Down") }
        };

    public static string GetPrimaryName(this HarptosMonth month) => DisplayNames[month].Primary;

    public static string? GetAlias(this HarptosMonth month) => DisplayNames[month].Alias;

    public static string GetFormalName(this HarptosMonth month)
    {
        (string primary, string? alias) = DisplayNames[month];
        return alias == null ? primary : $"{primary} ({alias})";
    }
}
