using AmiaReforged.Races.Races.Utils;
using NWN.Core;

namespace AmiaReforged.Races.Races.Script.SubraceTemplates
{
    public class AquaticElfOption : ISubraceApplier
    {
        public void Apply(uint nwnObjectId)
        {

            if (NWScript.GetItemPossessedBy(nwnObjectId, "platinum_token") == NWScript.OBJECT_INVALID)
            {
                NWScript.SendMessageToPC(nwnObjectId, "This subrace requires DM permission to play.");
                return;
            }

            if (NWScript.GetRacialType(nwnObjectId) != NWScript.RACIAL_TYPE_ELF)
            {
                NWScript.SendMessageToPC(nwnObjectId, "Aquatic Elf only works with the Moon Elf base race.");
                return;
            }

            NWScript.CreateItemOnObject(TemplateItem.TemplateItemResRef, nwnObjectId);

            SetSubraceModifiers(nwnObjectId);

            TemplateRunner templateRunner = new();

            TemplateRunner.Run(nwnObjectId);
        }

        private static void SetSubraceModifiers(uint nwnObjectId)
        {
            TemplateItem.SetSubRace(nwnObjectId, "Aquatic Elf");
            TemplateItem.SetIntMod(nwnObjectId, -2);
            TemplateItem.SetConMod(nwnObjectId, 2);
        }
    }
}