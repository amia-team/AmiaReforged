using AmiaReforged.Races.Races.Utils;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Races.Races.Script.SubraceTemplates
{
    public class CentaurOption : ISubraceApplier
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
                NWScript.SendMessageToPC(nwnObjectId, "Centaur only works with the Moon Elf base race.");
                return;
            }

            NWScript.CreateItemOnObject(TemplateItem.TemplateItemResRef, nwnObjectId);
            NWScript.SetCreatureAppearanceType(nwnObjectId, 6);
            NWScript.SetPhenoType(34, nwnObjectId);

            SetSubraceModifiers(nwnObjectId);

            TemplateRunner? templateRunner = new TemplateRunner();

            TemplateRunner.Run(nwnObjectId);
            CreaturePlugin.SetRacialType(nwnObjectId, NWScript.RACIAL_TYPE_HUMANOID_MONSTROUS);
            CreaturePlugin.AddFeatByLevel(nwnObjectId, 228, 1);
            CreaturePlugin.AddFeatByLevel(nwnObjectId, 194, 1);


        }

        private static void SetSubraceModifiers(uint nwnObjectId)
        {
            TemplateItem.SetSubRace(nwnObjectId, "Centaur");
            TemplateItem.SetIntMod(nwnObjectId, -2);
            TemplateItem.SetDexMod(nwnObjectId, -2);
            TemplateItem.SetStrMod(nwnObjectId, 1);
            TemplateItem.SetConMod(nwnObjectId, 3);
        }
    }
}