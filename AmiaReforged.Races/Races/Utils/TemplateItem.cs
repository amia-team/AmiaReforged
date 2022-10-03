using static NWN.Core.NWScript;

namespace Amia.Racial.Races.Utils
{
    public static class TemplateItem
    {
        public const string TemplateItemResRef = "char_template";

        private const string StrModVar = "str_mod";
        private const string DexModVar = "dex_mod";
        private const string ConModVar = "con_mod";
        private const string IntModVar = "int_mod";
        private const string WisModVar = "wis_mod";
        private const string ChaModVar = "cha_mod";
        private const string SubRaceVar = "subrace";

        public const string InitializedVar = "template_initialized";

        public static bool CreatureDoesNotHaveTemplate(uint creature)
        {
            return GetTemplateItemFromCreature(creature) == OBJECT_INVALID;
        }

        private static uint GetTemplateItemFromCreature(uint creature)
        {
            return GetItemPossessedBy(creature, TemplateItemResRef);
        }

        public static void SetStrMod(uint creature, int value)
        {
            SetLocalInt(GetTemplateItemFromCreature(creature), StrModVar, value);
        }

        public static void SetDexMod(uint creature, int value)
        {
            SetLocalInt(GetTemplateItemFromCreature(creature), DexModVar, value);
        }

        public static void SetConMod(uint creature, int value)
        {
            SetLocalInt(GetTemplateItemFromCreature(creature), ConModVar, value);
        }

        public static void SetIntMod(uint creature, int value)
        {
            SetLocalInt(GetTemplateItemFromCreature(creature), IntModVar, value);
        }

        public static void SetWisMod(uint creature, int value)
        {
            SetLocalInt(GetTemplateItemFromCreature(creature), WisModVar, value);
        }

        public static void SetChaMod(uint creature, int value)
        {
            SetLocalInt(GetTemplateItemFromCreature(creature), ChaModVar, value);
        }

        public static void SetSubRace(uint creature, string value)
        {
            SetLocalString(GetTemplateItemFromCreature(creature), SubRaceVar, value);
        }

        public static bool Initialized(in uint nwnObjectId)
        {
            return GetLocalInt(GetTemplateItemFromCreature(nwnObjectId), InitializedVar) == TRUE;
        }
    }
}