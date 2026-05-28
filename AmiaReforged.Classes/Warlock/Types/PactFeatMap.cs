using AmiaReforged.Classes.Warlock.Constants;
using Anvil.API;

namespace AmiaReforged.Classes.Warlock.Types;

public static class PactFeatMap
{
    private static readonly Dictionary<PactType, Feat[]> FeatsByPact = new()
    {
        [PactType.Aberrant] = [WarlockFeat.LoudDecay, WarlockFeat.HollowingEssence],
        [PactType.Celestial] = [WarlockFeat.LightsCalling, WarlockFeat.RadiantEssence],
        [PactType.Elemental] = [WarlockFeat.PrimordialGust, WarlockFeat.StormEssence],
        [PactType.Fey] = [WarlockFeat.DancingPlague, WarlockFeat.WitchwoodEssence],
        [PactType.Fiend] = [WarlockFeat.BindingOfMaggots, WarlockFeat.GluttonousEssence],
        [PactType.Slaad] = [WarlockFeat.FrogDrop, WarlockFeat.EntropicEssence],
    };

    public static Feat[] GetFeats(PactType pact) => FeatsByPact.TryGetValue(pact, out Feat[]? feats) ? feats : [];
}
