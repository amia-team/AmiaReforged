using AmiaReforged.Races.Races.Utils;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Races.Races.Script.SubraceTemplates;

// [ScriptName("race_init_fey")]
public class FeytouchedOption : ISubraceApplier
{
    public void Apply(uint nwnObjectId)
    {

        NWScript.CreateItemOnObject(TemplateItem.TemplateItemResRef, nwnObjectId);

        SetSubRaceMod(nwnObjectId);

        TemplateRunner templateRunner = new();

        TemplateRunner.Run(nwnObjectId);

        CreaturePlugin.SetRacialType(nwnObjectId, NWScript.RACIAL_TYPE_FEY);
        CreaturePlugin.AddFeatByLevel(nwnObjectId,354,1);
    }

    private static void SetSubRaceMod(uint nwnObjectId)
    {
        TemplateItem.SetSubRace(nwnObjectId, "Feytouched");
        TemplateItem.SetChaMod(nwnObjectId, 2);
        TemplateItem.SetConMod(nwnObjectId, -2);
    }
}