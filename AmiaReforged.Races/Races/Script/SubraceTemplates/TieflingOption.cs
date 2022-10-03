using Amia.Racial.Races.Utils;
using NWN.Core;
using NWN.Core.NWNX;

namespace Amia.Racial.Races.Script.SubraceTemplates
{
    public class TieflingOption : ISubraceApplier
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
            TemplateItem.SetSubRace(nwnObjectId, "Tiefling");
            TemplateItem.SetIntMod(nwnObjectId, 2);
            TemplateItem.SetChaMod(nwnObjectId, -2);
        }
    }
}