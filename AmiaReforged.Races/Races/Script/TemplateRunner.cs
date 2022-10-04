using AmiaReforged.Races.Races.Types.RacialTemplates;
using AmiaReforged.Races.Races.Utils;
using NWN.Core;

namespace AmiaReforged.Races.Races.Script
{
    public class TemplateRunner 
    {
        public static void Run(uint nwnObjectId)
        {
            if (TemplateItem.CreatureDoesNotHaveTemplate(nwnObjectId)) return;
            if (TemplateItem.Initialized(nwnObjectId)) return;

            RacialTemplate template = TemplateMaker.SetupStats(nwnObjectId);

            template.Apply();
        }
    }

    public static class TemplateMaker
    {
        public static RacialTemplate SetupStats(in uint nwnObjectId)
        {
            uint templateItem = NWScript.GetItemPossessedBy(nwnObjectId, TemplateItem.TemplateItemResRef);

            return new RacialTemplate(nwnObjectId)
            {
                StrBonus = NWScript.GetLocalInt(templateItem, "str_mod"),
                DexBonus = NWScript.GetLocalInt(templateItem, "dex_mod"),
                ConBonus = NWScript.GetLocalInt(templateItem, "con_mod"),
                IntBonus = NWScript.GetLocalInt(templateItem, "int_mod"),
                WisBonus = NWScript.GetLocalInt(templateItem, "wis_mod"),
                ChaBonus = NWScript.GetLocalInt(templateItem, "cha_mod"),
                SubRace = NWScript.GetLocalString(templateItem, "subrace")
            };
        }
    }
}