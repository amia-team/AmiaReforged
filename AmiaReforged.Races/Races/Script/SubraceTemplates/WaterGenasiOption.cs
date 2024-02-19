using AmiaReforged.Races.Races.Utils;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Races.Races.Script.SubraceTemplates;

public class WaterGenasiOption : ISubraceApplier
{
    public void Apply(uint nwnObjectId)
    {
        NWScript.CreateItemOnObject(TemplateItem.TemplateItemResRef, nwnObjectId);

        SetSubRaceMod(nwnObjectId);

        TemplateRunner.Run(nwnObjectId);

        CreaturePlugin.SetRacialType(nwnObjectId, NWScript.RACIAL_TYPE_OUTSIDER);
        CreaturePlugin.AddFeatByLevel(nwnObjectId, 228, 1);
    }

    private static void SetSubRaceMod(uint nwnObjectId)
    {
        TemplateItem.SetSubRace(nwnObjectId, "Water Genasi");
        TemplateItem.SetConMod(nwnObjectId, 2);
        TemplateItem.SetChaMod(nwnObjectId, -2);
    }
}