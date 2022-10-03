using Amia.Racial.Races.Utils;
using JetBrains.Annotations;
using NLog;
using NWN.Amia.Main.Managed.Races.Utils;
using NWN.Core;
using NWN.Core.NWNX;

namespace Amia.Racial.Races.Types.RacialTemplates
{
    public class RacialTemplate : ICharacterTemplate
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private const string TemplateInitialized = TemplateItem.InitializedVar;
        private readonly uint _nwnObjectId;

        public RacialTemplate(in uint nwnObjectId)
        {
            _nwnObjectId = nwnObjectId;
        }

        public RacialTemplate()
        {
        }

        [UsedImplicitly] public string SubRace { get; set; }
        [UsedImplicitly] public int StrBonus { get; set; }
        [UsedImplicitly] public int IntBonus { get; set; }
        [UsedImplicitly] public int ConBonus { get; set; }
        [UsedImplicitly] public int ChaBonus { get; set; }
        [UsedImplicitly] public int DexBonus { get; set; }
        [UsedImplicitly] public int WisBonus { get; set; }

        [UsedImplicitly] public List<IntPtr> TemplateEffects { get; set; }

        public void Apply()
        {
            if (!RaceAdjustmentUtils.BaseRaceDict.ContainsKey(NWScript.GetRacialType(_nwnObjectId)))
                NWScript.SendMessageToPC(_nwnObjectId, "Racial templates are only supported by the default NWN races.");

            RemoveBaseAbilitiesIfNotInitialized();

            ApplyAbilityModsIfNotInitialized();
        }

        private void RemoveBaseAbilitiesIfNotInitialized()
        {
            int race = NWScript.GetRacialType(_nwnObjectId);
            RacialTemplate baseRaceTemplate;

            try
            {
                baseRaceTemplate = RaceAdjustmentUtils.BaseRaceDict[race];
            }
            catch (Exception)
            {
                return;
            }

            uint templateItem = NWScript.GetItemPossessedBy(_nwnObjectId, "char_template");

            const string offsetsApplied = "offsets_applied";
            bool raceIsNullOrOffsetSet = NWScript.GetLocalInt(templateItem, offsetsApplied) == NWScript.TRUE;

            if (raceIsNullOrOffsetSet) return;


            ApplyStatIfBonusNotZero(NWScript.ABILITY_STRENGTH, baseRaceTemplate.StrBonus);
            ApplyStatIfBonusNotZero(NWScript.ABILITY_CONSTITUTION, baseRaceTemplate.ConBonus);
            ApplyStatIfBonusNotZero(NWScript.ABILITY_DEXTERITY, baseRaceTemplate.DexBonus);
            ApplyStatIfBonusNotZero(NWScript.ABILITY_INTELLIGENCE, baseRaceTemplate.IntBonus);
            ApplyStatIfBonusNotZero(NWScript.ABILITY_CHARISMA, baseRaceTemplate.ChaBonus);
            ApplyStatIfBonusNotZero(NWScript.ABILITY_WISDOM, baseRaceTemplate.WisBonus);

            NWScript.SetLocalInt(templateItem, offsetsApplied, NWScript.TRUE);
        }

        private void ApplyStatIfBonusNotZero(int ability, int bonus)
        {
            Log.Info(
                $"Ability before modification: {CreaturePlugin.GetRawAbilityScore(_nwnObjectId, ability)}");
            int totalToSet = CreaturePlugin.GetRawAbilityScore(_nwnObjectId, ability) + bonus;
            CreaturePlugin.SetRawAbilityScore(_nwnObjectId, ability, totalToSet);
            Log.Info(
                $"Ability after modification: {CreaturePlugin.GetRawAbilityScore(_nwnObjectId, ability)}");
        }

        private void ApplyAbilityModsIfNotInitialized()
        {
            uint templateItem = NWScript.GetItemPossessedBy(_nwnObjectId, "char_template");

            if (NWScript.GetLocalInt(templateItem, TemplateInitialized) == NWScript.TRUE) return;

            ApplyStatIfBonusNotZero(NWScript.ABILITY_STRENGTH, StrBonus);
            ApplyStatIfBonusNotZero(NWScript.ABILITY_CONSTITUTION, ConBonus);
            ApplyStatIfBonusNotZero(NWScript.ABILITY_DEXTERITY, DexBonus);
            ApplyStatIfBonusNotZero(NWScript.ABILITY_INTELLIGENCE, IntBonus);
            ApplyStatIfBonusNotZero(NWScript.ABILITY_CHARISMA, ChaBonus);
            ApplyStatIfBonusNotZero(NWScript.ABILITY_WISDOM, WisBonus);

            NWScript.SetSubRace(_nwnObjectId, SubRace);

            NWScript.SetLocalInt(templateItem, TemplateInitialized, NWScript.TRUE);
        }
    }
}