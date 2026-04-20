using AmiaReforged.Classes.Warlock.Constants;

namespace AmiaReforged.Classes.Warlock.Types;

/// <summary>
/// Enum for pacts whose value matches the feat id
/// </summary>
public enum PactType
{
    Aberrant = WarlockFeat.AberrantPact,
    Celestial = WarlockFeat.CelestialPact,
    Fey = WarlockFeat.FeyPact,
    Fiend = WarlockFeat.FiendPact,
    Elemental = WarlockFeat.ElementalPact,
    Slaad = WarlockFeat.SlaadPact,
}
