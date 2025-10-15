using AmiaReforged.Races.Races.Utils;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Races.Races.Script.SubraceTemplates;

public class HalfVampireOption : ISubraceApplier
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

        TemplateRunner.Run(nwnObjectId);

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
