using AmiaReforged.Classes.Warlock.Constants;

namespace AmiaReforged.Classes.Warlock.Types;

/// <summary>
/// Enum for pacts whose value matches the feat id
/// </summary>
public enum PactType
{
    Aberrant = WarlockFeats.AberrantPact,
    Celestial = WarlockFeats.CelestialPact,
    Fey = WarlockFeats.FeyPact,
    Fiend = WarlockFeats.FiendPact,
    Elemental = WarlockFeats.ElementalPact,
    Slaad = WarlockFeats.SlaadPact,
}
