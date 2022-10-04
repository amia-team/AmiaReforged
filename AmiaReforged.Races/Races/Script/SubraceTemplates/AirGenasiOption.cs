using AmiaReforged.Races.Races.Utils;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Races.Races.Script.SubraceTemplates
{
    public class AirGenasiOption : ISubraceApplier
    {
        public void Apply(uint nwnObjectId)
        {

            NWScript.CreateItemOnObject(TemplateItem.TemplateItemResRef, nwnObjectId);

            CreaturePlugin.SetRacialType(nwnObjectId, NWScript.RACIAL_TYPE_OUTSIDER);
            CreaturePlugin.AddFeatByLevel(nwnObjectId,228,1);
            SetSubRaceMod(nwnObjectId);

            TemplateRunner templateRunner = new();

            TemplateRunner.Run(nwnObjectId);
        }

        private static void SetSubRaceMod(uint nwnObjectId)
        {
            TemplateItem.SetSubRace(nwnObjectId, "Air Genasi");
            TemplateItem.SetDexMod(nwnObjectId, 2);
            TemplateItem.SetIntMod(nwnObjectId, 2);
            TemplateItem.SetChaMod(nwnObjectId, -2);
            TemplateItem.SetWisMod(nwnObjectId, -2);
        }
    }
}