using AmiaReforged.Races.Races.Utils;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Races.Races.Script.SubraceTemplates
{
    public class KenkuOption : ISubraceApplier
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
                NWScript.SendMessageToPC(nwnObjectId, "Kenku only works with the Non-Regional Human base race.");
                return;
            }

            NWScript.CreateItemOnObject(TemplateItem.TemplateItemResRef, nwnObjectId);

            SetSubraceModifiers(nwnObjectId);

            TemplateRunner templateRunner = new();
            CreaturePlugin.AddFeatByLevel(nwnObjectId,354,1);

            TemplateRunner.Run(nwnObjectId);
        }

        private static void SetSubraceModifiers(uint nwnObjectId)
        {
            TemplateItem.SetSubRace(nwnObjectId, "Kenku");
            TemplateItem.SetDexMod(nwnObjectId, 2);
            TemplateItem.SetStrMod(nwnObjectId, -2);
        }
    }
}