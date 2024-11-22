using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(TrapPlacementService))]
public class TrapPlacementService
{
    private const string ElectricTrapComponent = "itm_sc_ecoil";
    private const string FireTrapComponent = "itm_sc_fivalve";
    private const string SonicTrapComponent = "itm_sc_sintensi";
    private const string ColdTrapComponent = "itm_sc_cxchangr";
    private const string GasTrapComponent = "itm_sc_gxcan";
    private const string HolyTrapComponent = "itm_sc_hatomis";
    private const string TangleTrapComponent = "itm_sc_trazors";
    private const string NegativeTrapComponent = "itm_sc_ninducer";
    private const string SpikeTrapComponent = "itm_sc_aconcent";
    
    private const string TrapPlacementScript = "evnt_set_trap";

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly Dictionary<string, Dictionary<TrapBaseType, TrapBaseType>> _trapDictionary;
    private readonly Dictionary<TrapBaseType, string> _trapComponentDictionary;

    public TrapPlacementService()
    {
        EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_SET_AFTER", TrapPlacementScript);

        _trapDictionary = new Dictionary<string, Dictionary<TrapBaseType, TrapBaseType>>
        {
            {
                ElectricTrapComponent, new Dictionary<TrapBaseType, TrapBaseType>
                {
                    { TrapBaseType.MinorElectrical, TrapBaseType.AverageElectrical },
                    { TrapBaseType.AverageElectrical, TrapBaseType.StrongElectrical },
                    { TrapBaseType.StrongElectrical, TrapBaseType.DeadlyElectrical },
                    { TrapBaseType.DeadlyElectrical, TrapBaseType.EpicElectrical }
                }
            },
            {
                FireTrapComponent, new Dictionary<TrapBaseType, TrapBaseType>
                {
                    { TrapBaseType.MinorFire, TrapBaseType.AverageFire },
                    { TrapBaseType.AverageFire, TrapBaseType.StrongFire },
                    { TrapBaseType.StrongFire, TrapBaseType.DeadlyFire },
                    { TrapBaseType.DeadlyFire, TrapBaseType.EpicFire }
                }
            },
            {
                SonicTrapComponent, new Dictionary<TrapBaseType, TrapBaseType>
                {
                    { TrapBaseType.MinorSonic, TrapBaseType.AverageSonic },
                    { TrapBaseType.AverageSonic, TrapBaseType.StrongSonic },
                    { TrapBaseType.StrongSonic, TrapBaseType.DeadlySonic },
                    { TrapBaseType.DeadlySonic, TrapBaseType.EpicSonic }
                }
            },
            {
                ColdTrapComponent, new Dictionary<TrapBaseType, TrapBaseType>
                {
                    { TrapBaseType.MinorFrost, TrapBaseType.AverageFrost },
                    { TrapBaseType.AverageFrost, TrapBaseType.StrongFrost },
                    { TrapBaseType.StrongFrost, TrapBaseType.DeadlyFrost },
                    { TrapBaseType.DeadlyFrost, TrapBaseType.EpicFrost }
                }
            },
            {
                GasTrapComponent, new Dictionary<TrapBaseType, TrapBaseType>
                {
                    { TrapBaseType.MinorGas, TrapBaseType.AverageGas },
                    { TrapBaseType.AverageGas, TrapBaseType.StrongGas },
                    { TrapBaseType.StrongGas, TrapBaseType.DeadlyGas }
                }
            },
            {
                HolyTrapComponent, new Dictionary<TrapBaseType, TrapBaseType>
                {
                    { TrapBaseType.MinorHoly, TrapBaseType.AverageHoly },
                    { TrapBaseType.AverageHoly, TrapBaseType.StrongHoly },
                    { TrapBaseType.StrongHoly, TrapBaseType.DeadlyHoly }
                }
            },
            {
                TangleTrapComponent, new Dictionary<TrapBaseType, TrapBaseType>
                {
                    { TrapBaseType.MinorTangle, TrapBaseType.AverageTangle },
                    { TrapBaseType.AverageTangle, TrapBaseType.StrongTangle },
                    { TrapBaseType.StrongTangle, TrapBaseType.DeadlyTangle }
                }
            },
            {
                NegativeTrapComponent, new Dictionary<TrapBaseType, TrapBaseType>
                {
                    { TrapBaseType.MinorNegative, TrapBaseType.AverageNegative },
                    { TrapBaseType.AverageNegative, TrapBaseType.StrongNegative },
                    { TrapBaseType.StrongNegative, TrapBaseType.DeadlyNegative }
                }
            },
            {
                SpikeTrapComponent, new Dictionary<TrapBaseType, TrapBaseType>
                {
                    { TrapBaseType.MinorSpike, TrapBaseType.AverageSpike },
                    { TrapBaseType.AverageSpike, TrapBaseType.StrongSpike },
                    { TrapBaseType.StrongSpike, TrapBaseType.DeadlySpike }
                }
            }
        };

        _trapComponentDictionary = new Dictionary<TrapBaseType, string>
        {
            { TrapBaseType.MinorElectrical, ElectricTrapComponent },
            { TrapBaseType.AverageElectrical, ElectricTrapComponent },
            { TrapBaseType.StrongElectrical, ElectricTrapComponent },
            { TrapBaseType.DeadlyElectrical, ElectricTrapComponent },
            { TrapBaseType.EpicElectrical, ElectricTrapComponent },
            { TrapBaseType.MinorFire, FireTrapComponent },
            { TrapBaseType.AverageFire, FireTrapComponent },
            { TrapBaseType.StrongFire, FireTrapComponent },
            { TrapBaseType.DeadlyFire, FireTrapComponent },
            { TrapBaseType.EpicFire, FireTrapComponent },
            { TrapBaseType.MinorSonic, SonicTrapComponent },
            { TrapBaseType.AverageSonic, SonicTrapComponent },
            { TrapBaseType.StrongSonic, SonicTrapComponent },
            { TrapBaseType.DeadlySonic, SonicTrapComponent },
            { TrapBaseType.EpicSonic, SonicTrapComponent },
            { TrapBaseType.MinorFrost, ColdTrapComponent },
            { TrapBaseType.AverageFrost, ColdTrapComponent },
            { TrapBaseType.StrongFrost, ColdTrapComponent },
            { TrapBaseType.DeadlyFrost, ColdTrapComponent },
            { TrapBaseType.EpicFrost, ColdTrapComponent },
            { TrapBaseType.MinorGas, GasTrapComponent },
            { TrapBaseType.AverageGas, GasTrapComponent },
            { TrapBaseType.StrongGas, GasTrapComponent },
            { TrapBaseType.DeadlyGas, GasTrapComponent },
            { TrapBaseType.MinorHoly, HolyTrapComponent },
            { TrapBaseType.AverageHoly, HolyTrapComponent },
            { TrapBaseType.StrongHoly, HolyTrapComponent },
            { TrapBaseType.DeadlyHoly, HolyTrapComponent },
            { TrapBaseType.MinorTangle, TangleTrapComponent },
            { TrapBaseType.AverageTangle, TangleTrapComponent },
            { TrapBaseType.StrongTangle, TangleTrapComponent },
            { TrapBaseType.DeadlyTangle, TangleTrapComponent },
            { TrapBaseType.MinorNegative, NegativeTrapComponent },
            { TrapBaseType.AverageNegative, NegativeTrapComponent },
            { TrapBaseType.StrongNegative, NegativeTrapComponent },
            { TrapBaseType.DeadlyNegative, NegativeTrapComponent },
            { TrapBaseType.MinorSpike, SpikeTrapComponent },
            { TrapBaseType.AverageSpike, SpikeTrapComponent },
            { TrapBaseType.StrongSpike, SpikeTrapComponent },
            { TrapBaseType.DeadlySpike, SpikeTrapComponent }
        };
    }

    [ScriptHandler(TrapPlacementScript)]
    public void OnSetTrapAfter(CallInfo info)
    {
        if (info.ObjectSelf is null)
        {
            Log.Error("Couldn't get object self: Creature is null");
            return;
        }
        
        if (!info.ObjectSelf.IsLoginPlayerCharacter(out NwPlayer? player))
        {
            return;
        }

        NwCreature creature = player.LoginCreature!;

        NwTrigger? trigger = creature.GetNearestObjectsByType<NwTrigger>().Where(t => t.TrapCreator == player).FirstOrDefault();
        if(trigger is null)
        {
            Log.Info("Couldn't get trigger: Trigger is null");
            return;
        }

        TrapBaseType trapType = trigger.TrapBaseType;

        if (HasNoUpgradeComponentFor(trapType, creature)) return;

        CreateTrapUpgrade(trapType, trigger, creature);
    }

    private bool HasNoUpgradeComponentFor(TrapBaseType trapType, NwCreature creature)
    {
        string componentResRef = _trapComponentDictionary[trapType];
        return creature.Inventory.Items.Any(i => i.ResRef == componentResRef);
    }

    private void CreateTrapUpgrade(TrapBaseType trapType, NwTrigger trigger, NwCreature creature)
    {
        Log.Info("Setting trap upgrade");
        TrapBaseType trapUpgrade = _trapDictionary[_trapComponentDictionary[trapType]][trapType];

        string componentResRef = _trapComponentDictionary[trapUpgrade];

        NwItem? component = creature.Inventory.Items.FirstOrDefault(i => i.ResRef == componentResRef);

        if (component is null)
        {
            Log.Info("Couldn't find component in inventory: Component is null");
            return;
        }

        component.Destroy();
        NWScript.CreateTrapAtLocation((int)trapUpgrade, trigger.Location!);
        trigger.Destroy();

        if (!creature.IsPlayerControlled(out NwPlayer? player)) return;

        player.SendServerMessage("Your trap has been upgraded using a component from your inventory.");
    }
}