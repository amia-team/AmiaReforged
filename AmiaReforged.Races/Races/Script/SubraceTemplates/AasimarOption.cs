using AmiaReforged.Races.Races.Utils;
using NLog;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Races.Races.Script.SubraceTemplates
{
    public class AasimarOption : ISubraceApplier
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public void Apply(uint nwnObjectId)
        {
            uint template = NWScript.CreateItemOnObject(TemplateItem.TemplateItemResRef, nwnObjectId);
            Log.Info($"Template created successfully on {NWScript.GetName(nwnObjectId)}? {NWScript.GetIsObjectValid(template) == NWScript.TRUE}");
            SetSubraceMods(nwnObjectId);
            
            TemplateRunner.Run(nwnObjectId);

            CreaturePlugin.SetRacialType(nwnObjectId, NWScript.RACIAL_TYPE_OUTSIDER);
            CreaturePlugin.AddFeatByLevel(nwnObjectId, 228, 1);

        }

        private static void SetSubraceMods(uint nwnObjectId)
        {
            TemplateItem.SetSubRace(nwnObjectId, "Aasimar");
            TemplateItem.SetWisMod(nwnObjectId, 2);
            TemplateItem.SetConMod(nwnObjectId, -2);
        }
    }
}