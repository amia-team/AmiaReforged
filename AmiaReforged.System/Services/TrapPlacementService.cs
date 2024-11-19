using System.Collections.ObjectModel;
using Anvil.API;
using Anvil.API.Events;
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

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private Dictionary<string, Dictionary<TrapBaseType, TrapBaseType>> _trapDictionary;

    public TrapPlacementService()
    {
        EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_SET_AFTER", "TrapPlacement");

        _trapDictionary = new Dictionary<string, Dictionary<TrapBaseType, TrapBaseType>>();

        _trapDictionary.Add(ElectricTrapComponent, new Dictionary<TrapBaseType, TrapBaseType>
        {
            { TrapBaseType.MinorElectrical, TrapBaseType.AverageElectrical },
            { TrapBaseType.AverageElectrical, TrapBaseType.StrongElectrical },
            { TrapBaseType.StrongElectrical, TrapBaseType.DeadlyElectrical },
            { TrapBaseType.DeadlyElectrical, TrapBaseType.EpicElectrical }
        });

        _trapDictionary.Add(FireTrapComponent, new Dictionary<TrapBaseType, TrapBaseType>
        {
            { TrapBaseType.MinorFire, TrapBaseType.AverageFire },
            { TrapBaseType.AverageFire, TrapBaseType.StrongFire },
            { TrapBaseType.StrongFire, TrapBaseType.DeadlyFire },
            { TrapBaseType.DeadlyFire, TrapBaseType.EpicFire }
        });

        _trapDictionary.Add(SonicTrapComponent, new Dictionary<TrapBaseType, TrapBaseType>
        {
            { TrapBaseType.MinorSonic, TrapBaseType.AverageSonic },
            { TrapBaseType.AverageSonic, TrapBaseType.StrongSonic },
            { TrapBaseType.StrongSonic, TrapBaseType.DeadlySonic },
            { TrapBaseType.DeadlySonic, TrapBaseType.EpicSonic }
        });

        _trapDictionary.Add(ColdTrapComponent, new Dictionary<TrapBaseType, TrapBaseType>
        {
            { TrapBaseType.MinorFrost, TrapBaseType.AverageFrost },
            { TrapBaseType.AverageFrost, TrapBaseType.StrongFrost },
            { TrapBaseType.StrongFrost, TrapBaseType.DeadlyFrost },
            { TrapBaseType.DeadlyFrost, TrapBaseType.EpicFrost }
        });

        _trapDictionary.Add(GasTrapComponent, new Dictionary<TrapBaseType, TrapBaseType>
        {
            { TrapBaseType.MinorGas, TrapBaseType.AverageGas },
            { TrapBaseType.AverageGas, TrapBaseType.StrongGas },
            { TrapBaseType.StrongGas, TrapBaseType.DeadlyGas }
        });

        _trapDictionary.Add(HolyTrapComponent, new Dictionary<TrapBaseType, TrapBaseType>
        {
            { TrapBaseType.MinorHoly, TrapBaseType.AverageHoly },
            { TrapBaseType.AverageHoly, TrapBaseType.StrongHoly },
            { TrapBaseType.StrongHoly, TrapBaseType.DeadlyHoly }
        });
    }

    [ScriptHandler("TrapPlacement")]
    public void OnSetTrap(CallInfo info)
    {
        string obj = EventsPlugin.GetEventData("OBJECT_SELF");
        NwCreature? creature = NWScript.StringToObject(obj).ToNwObject<NwCreature>();
        if (creature is null)
        {
            Log.Info("Couldn't get creature: Creature is null");
            return;
        }

        string trap = EventsPlugin.GetEventData("TRAP_OBJECT_ID");
        NwTrigger? trigger = NWScript.StringToObject(trap).ToNwObject<NwTrigger>();
        if (trigger is null)
        {
            Log.Info("Couldn't get recently placed trap: Trap is null");
            return;
        }

        TrapBaseType trapType = trigger.TrapBaseType;

        if (HasNoUpgradeComponentFor(trapType, creature)) return;
    }

    private bool HasNoUpgradeComponentFor(TrapBaseType trapType, NwCreature? creature)
    {
        // List<NwItem> trapItems = 
        return false;
    }
}