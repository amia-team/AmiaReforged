using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog.Targets;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Services.Defender;

public class DefendersDuty
{
    private const float DefenderDamage = 0.25f;

    private NwPlayer Defender { get; }
    private NwCreature Target { get; }

    private readonly SchedulerService _scheduler;

    private ScheduledTask? _deleteSoakDamageTask;

    public DefendersDuty(NwPlayer defender, NwCreature target, SchedulerService scheduler)
    {
        Defender = defender;
        Target = target;
        _scheduler = scheduler;
    }

    public void Apply()
    {
        const float duration = 7.0f;

        Target.OnCreatureDamage += SoakDamage;

        if (!Target.IsPlayerControlled(out NwPlayer? otherPlayer))
        {
            // This is a creature, not a player. We don't need to do anything special. This is just to get the player object for the OnClientLeave event.
        }

        if (otherPlayer != null)
            otherPlayer.OnClientLeave += CancelDuty;
        Defender.OnClientLeave += CancelDuty;

        Defender.LoginCreature?.JumpToObject(Target);
        Defender.LoginCreature?.SpeakString("*jumps to protecc fren :)))))*");

        _deleteSoakDamageTask =
            _scheduler.Schedule(() =>
            {
                Target.OnCreatureDamage -= SoakDamage;
                if (otherPlayer != null)
                {
                    otherPlayer.OnClientLeave -= CancelDuty;
                }

                Defender.OnClientLeave -= CancelDuty;
            }, TimeSpan.FromSeconds(duration));
    }

    private void CancelDuty(ModuleEvents.OnClientLeave obj)
    {
        Target.OnCreatureDamage -= SoakDamage;

        _deleteSoakDamageTask?.Cancel();
    }

    private void SoakDamage(OnCreatureDamage obj)
    {
        DamageData defenderDamageData = new()
        {
            iBludgeoning = (int)(obj.DamageData.GetDamageByType(DamageType.Bludgeoning) * DefenderDamage),
            iPierce = (int)(obj.DamageData.GetDamageByType(DamageType.Piercing) * DefenderDamage),
            iSlash = (int)(obj.DamageData.GetDamageByType(DamageType.Slashing) * DefenderDamage),
            iMagical = (int)(obj.DamageData.GetDamageByType(DamageType.Magical) * DefenderDamage),
            iAcid = (int)(obj.DamageData.GetDamageByType(DamageType.Acid) * DefenderDamage),
            iCold = (int)(obj.DamageData.GetDamageByType(DamageType.Cold) * DefenderDamage),
            iDivine = (int)(obj.DamageData.GetDamageByType(DamageType.Divine) * DefenderDamage),
            iElectrical = (int)(obj.DamageData.GetDamageByType(DamageType.Electrical) * DefenderDamage),
            iFire = (int)(obj.DamageData.GetDamageByType(DamageType.Fire) * DefenderDamage),
            iNegative = (int)(obj.DamageData.GetDamageByType(DamageType.Negative) * DefenderDamage),
            iPositive = (int)(obj.DamageData.GetDamageByType(DamageType.Positive) * DefenderDamage),
            iSonic = (int)(obj.DamageData.GetDamageByType(DamageType.Sonic) * DefenderDamage),
        };

        DamagePlugin.DealDamage(defenderDamageData, Defender.LoginCreature, obj.DamagedBy);

        obj.DamageData.SetDamageByType(DamageType.Bludgeoning,
            obj.DamageData.GetDamageByType(DamageType.Bludgeoning) - defenderDamageData.iBludgeoning);
        obj.DamageData.SetDamageByType(DamageType.Piercing,
            obj.DamageData.GetDamageByType(DamageType.Piercing) - defenderDamageData.iPierce);
        obj.DamageData.SetDamageByType(DamageType.Slashing,
            obj.DamageData.GetDamageByType(DamageType.Slashing) - defenderDamageData.iSlash);
        obj.DamageData.SetDamageByType(DamageType.Magical,
            obj.DamageData.GetDamageByType(DamageType.Magical) - defenderDamageData.iMagical);
        obj.DamageData.SetDamageByType(DamageType.Acid,
            obj.DamageData.GetDamageByType(DamageType.Acid) - defenderDamageData.iAcid);
        obj.DamageData.SetDamageByType(DamageType.Cold,
            obj.DamageData.GetDamageByType(DamageType.Cold) - defenderDamageData.iCold);
        obj.DamageData.SetDamageByType(DamageType.Divine,
            obj.DamageData.GetDamageByType(DamageType.Divine) - defenderDamageData.iDivine);
        obj.DamageData.SetDamageByType(DamageType.Electrical,
            obj.DamageData.GetDamageByType(DamageType.Electrical) - defenderDamageData.iElectrical);
        obj.DamageData.SetDamageByType(DamageType.Fire,
            obj.DamageData.GetDamageByType(DamageType.Fire) - defenderDamageData.iFire);
        obj.DamageData.SetDamageByType(DamageType.Negative,
            obj.DamageData.GetDamageByType(DamageType.Negative) - defenderDamageData.iNegative);
        obj.DamageData.SetDamageByType(DamageType.Positive,
            obj.DamageData.GetDamageByType(DamageType.Positive) - defenderDamageData.iPositive);
        obj.DamageData.SetDamageByType(DamageType.Sonic,
            obj.DamageData.GetDamageByType(DamageType.Sonic) - defenderDamageData.iSonic);
    }
}