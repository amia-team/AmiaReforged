using AmiaReforged.Races.Races.Utils;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Races.Races.Script.SubraceTemplates;

public class FeyriOption : ISubraceApplier
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
            NWScript.SendMessageToPC(nwnObjectId, "Fey'ri only works with the Moon Elf base race.");
            return;
        }

        NWScript.CreateItemOnObject(TemplateItem.TemplateItemResRef, nwnObjectId);

        SetSubraceModifiers(nwnObjectId);

        TemplateRunner? templateRunner = new();

        TemplateRunner.Run(nwnObjectId);
        CreaturePlugin.SetRacialType(nwnObjectId, NWScript.RACIAL_TYPE_OUTSIDER);
        CreaturePlugin.AddFeatByLevel(nwnObjectId, 228, 1);

    }

    private static void SetSubraceModifiers(uint nwnObjectId)
    {
        TemplateItem.SetSubRace(nwnObjectId, "Fey'ri");
    }
}