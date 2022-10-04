using AmiaReforged.Races.Races.Utils;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Races.Races.Script.SubraceTemplates
{
    public class LizardfolkOption : ISubraceApplier
    {
        public void Apply(uint nwnObjectId)
        {

            if (NWScript.GetItemPossessedBy(nwnObjectId, "platinum_token") == NWScript.OBJECT_INVALID)
            {
                NWScript.SendMessageToPC(nwnObjectId, "This subrace requires DM permission to play.");
                return;
            }

            if (NWScript.GetRacialType(nwnObjectId) != NWScript.RACIAL_TYPE_HUMAN)
            {
                NWScript.SendMessageToPC(nwnObjectId, "Lizardfolk only works with the Non-Regional Human base race.");
                return;
            }

            NWScript.CreateItemOnObject(TemplateItem.TemplateItemResRef, nwnObjectId);

            SetSubraceModifiers(nwnObjectId);

            TemplateRunner templateRunner = new();
            CreaturePlugin.SetRacialType(nwnObjectId, NWScript.RACIAL_TYPE_HUMANOID_REPTILIAN);

            TemplateRunner.Run(nwnObjectId);
            
        }

        private static void SetSubraceModifiers(uint nwnObjectId)
        {
            TemplateItem.SetSubRace(nwnObjectId, "Lizardfolk");
            TemplateItem.SetConMod(nwnObjectId, 2);
            TemplateItem.SetIntMod(nwnObjectId, -2);
        }
    }
} 