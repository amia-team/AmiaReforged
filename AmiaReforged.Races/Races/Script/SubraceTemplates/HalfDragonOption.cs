using Amia.Racial.Races.Utils;
using NWN.Core;
using NWN.Core.NWNX;

namespace Amia.Racial.Races.Script.SubraceTemplates
{
    public class HalfDragonOption : ISubraceApplier
    {
        public void Apply(uint nwnObjectId)
        {
            
            if (NWScript.GetItemPossessedBy(nwnObjectId, "platinum_token") == NWScript.OBJECT_INVALID)
            {
                NWScript.SendMessageToPC(nwnObjectId, "This subrace requires DM permission to play.");
                return;
            }

            NWScript.CreateItemOnObject(TemplateItem.TemplateItemResRef, nwnObjectId);

            SetSubRaceMod(nwnObjectId);

            TemplateRunner templateRunner = new();

            TemplateRunner.Run(nwnObjectId);

            CreaturePlugin.SetRacialType(nwnObjectId, NWScript.RACIAL_TYPE_DRAGON);
            CreaturePlugin.AddFeatByLevel(nwnObjectId,228,1);
            CreaturePlugin.AddFeatByLevel(nwnObjectId,240,1);
        }

        private static void SetSubRaceMod(uint nwnObjectId)
        {
            TemplateItem.SetSubRace(nwnObjectId, "Dragon");
            TemplateItem.SetStrMod(nwnObjectId, 2);
            TemplateItem.SetDexMod(nwnObjectId, -2);
        }
    }
}