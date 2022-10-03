using Anvil.API;
using JetBrains.Annotations;
using NLog;
using NWN.Amia.Main.Managed.Races;
using NWN.Core;

namespace Amia.Racial.Races.Script
{
    [UsedImplicitly]
    public static class HeritageFeatSetup
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private const string HeritageSetupVar = "heritage_setup";

        private static uint _nwnObject;
        private static int _playerRace;
        private static NwPlayer _player;
        private static uint _pckey;

        public static void Setup(NwPlayer player)
        {
            _nwnObject = player.LoginCreature;
            _player = player;
            _pckey = NWScript.GetItemPossessedBy(_nwnObject, "ds_pckey");
            _playerRace = ResolvePlayerRace();
            Log.Info($"Player race type: {_playerRace}");
            Log.Info($"Checking if {player.PlayerName} => {player.LoginCreature.Name} is a managed race.");
            
            bool playerRaceSupported = !PlayerRaceIsSupported();
            Log.Info($"Supported Race? {playerRaceSupported}");

            bool heritageFeatInitialized = HeritageFeatInitialized();
            Log.Info($"Heritage feat initialized already? {heritageFeatInitialized}");

            bool alreadyHasFeat = !HasHeritageFeat();
            Log.Info($"Already has heritage feat? {alreadyHasFeat}");
            
            if (playerRaceSupported || heritageFeatInitialized || alreadyHasFeat) return;

            PerformHeritageFeatSetup();
            FlagHeritageAsSetup();
        }

        private static bool HasHeritageFeat()
        {
            return NWScript.GetHasFeat(1238, _nwnObject) == NWScript.TRUE;
        }

        private static int ResolvePlayerRace() =>
            _player.LoginCreature.SubRace.ToLower() switch
            {
                "aasimar" => (int)ManagedRaces.RacialType.Aasimar,
                "tiefling" => (int)ManagedRaces.RacialType.Tiefling,
                "feytouched" => (int)ManagedRaces.RacialType.Feytouched,
                "feyri" => (int)ManagedRaces.RacialType.Feyri,
                "air genasi" => (int)ManagedRaces.RacialType.AirGenasi,
                "earth genasi" => (int)ManagedRaces.RacialType.EarthGenasi,
                "fire genasi" => (int)ManagedRaces.RacialType.FireGenasi,
                "water genasi" => (int)ManagedRaces.RacialType.WaterGenasi,
                "avariel" => (int)ManagedRaces.RacialType.Avariel,
                "lizardfolk" => (int)ManagedRaces.RacialType.Lizardfolk,
                "half dragon" => (int)ManagedRaces.RacialType.Halfdragon,
                "dragon" => (int)ManagedRaces.RacialType.Halfdragon,
                "centaur" => (int)ManagedRaces.RacialType.Centaur,
                "aquatic elf" => (int)ManagedRaces.RacialType.AquaticElf,
                "elfling" => (int)ManagedRaces.RacialType.Elfling,
                "shadovar" => (int)ManagedRaces.RacialType.Shadovar,
                _ => NWScript.GetRacialType(_nwnObject)
            };

        private static bool PlayerRaceIsSupported() => ManagedRaces.RaceHeritageAbilities.ContainsKey(_playerRace);

        private static bool HeritageFeatInitialized() =>
            NWScript.GetLocalInt(_pckey, HeritageSetupVar) == NWScript.TRUE;

        private static void PerformHeritageFeatSetup() => ManagedRaces.RaceHeritageAbilities[_playerRace].SetupStats(_player);

        private static void FlagHeritageAsSetup() => NWScript.SetLocalInt(_pckey, HeritageSetupVar, NWScript.TRUE);
    }
}