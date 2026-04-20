using AmiaReforged.Classes.Warlock.Constants;
using Anvil.API;

namespace AmiaReforged.Classes.Warlock.Types;

public static class PactFeatMap
{
    private static readonly Dictionary<PactType, Feat[]> FeatsByPact = new()
    {
        [PactType.Aberrant] = [WarlockFeat.LoudDecay],
        [PactType.Celestial] = [WarlockFeat.LightsCalling],
        [PactType.Fey] = [WarlockFeat.DancingPlague],
        [PactType.Fiend] = [WarlockFeat.BindingOfMaggots],
        [PactType.Elemental] = [WarlockFeat.PrimordialGust],
        [PactType.Slaad] = [WarlockFeat.FrogDrop],
    };

    public static Feat[] GetFeats(PactType pact) => FeatsByPact.TryGetValue(pact, out Feat[]? feats) ? feats : [];
}
