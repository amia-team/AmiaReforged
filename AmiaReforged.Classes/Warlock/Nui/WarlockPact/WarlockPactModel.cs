using AmiaReforged.Classes.Warlock.Types;
using Anvil.API;

namespace AmiaReforged.Classes.Warlock.Nui.WarlockPact;

public static class WarlockPactModel
{
    public record FeatData
    (
        string Name,
        string Description,
        string? Icon
    );

    public static FeatData? CreatePactData(PactType pact)
    {
        NwFeat? pactFeat = GetPactFeat(pact);
        if (pactFeat == null)
            return null;

        return new FeatData(
            pactFeat.Name.ToString(),
            pactFeat.Description.ToString(),
            pactFeat.IconResRef
        );
    }

    public static FeatData? CreatePactSpellData(int featId)
    {
        NwFeat? pactSpellFeat = NwFeat.FromFeatId(featId);
        if (pactSpellFeat == null)
            return null;

        return new FeatData(
            pactSpellFeat.Name.ToString(),
            pactSpellFeat.Description.ToString(),
            pactSpellFeat.IconResRef
        );
    }

    public static NwFeat? GetPactFeat(PactType pact) =>  NwFeat.FromFeatType((Feat)pact);
}
