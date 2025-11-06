using System.Collections.Generic;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Time;

public enum HarptosFestival
{
    Midwinter = 1,
    Greengrass,
    Midsummer,
    Shieldmeet,
    Highharvestide,
    FeastOfTheMoon
}

public static class HarptosFestivalExtensions
{
    private static readonly IReadOnlyDictionary<HarptosFestival, string> DisplayNames =
        new Dictionary<HarptosFestival, string>
        {
            { HarptosFestival.Midwinter, "Midwinter" },
            { HarptosFestival.Greengrass, "Greengrass" },
            { HarptosFestival.Midsummer, "Midsummer" },
            { HarptosFestival.Shieldmeet, "Shieldmeet" },
            { HarptosFestival.Highharvestide, "Highharvestide" },
            { HarptosFestival.FeastOfTheMoon, "Feast of the Moon" }
        };

    public static string GetDisplayName(this HarptosFestival festival) => DisplayNames[festival];
}
