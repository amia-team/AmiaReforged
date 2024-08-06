using AmiaReforged.Classes.EffectUtils;
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
    private const int OneRound = 1;

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
        ApplySoak();
        ApplyStunInSmallArea();
    }

    private void ApplySoak()
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

    private void ApplyStunInSmallArea()
    {
        // Set up the effect and difficulty class.
        IntPtr stunEffect = NWScript.EffectStunned();
        int difficulty = 10 +
                         (Defender.LoginCreature!.Classes.Single(c => c.Class.ClassType == ClassType.DwarvenDefender)
                             .Level / 2) + NWScript.GetAbilityModifier(NWScript.ABILITY_CONSTITUTION);
        
        // The stun should only last one round. 6 seconds is a very long time in PVP and PVE.
        float stunDur = NWScript.RoundsToSeconds(OneRound);
        
        // NWScript's internal library is used here to make it easier for non-C# devs to understand the 
        // way that this effect is applied. Anvil actually has its own Object Oriented way of doing things, but it was
        // felt that this is a good way to introduce new developers.
        uint objectInShape =
            NWScript.GetFirstObjectInShape(NWScript.SHAPE_SPHERE, NWScript.RADIUS_SIZE_LARGE, NWScript.GetLocation(Defender.LoginCreature));
        while (NWScript.GetIsObjectValid(objectInShape) == NWScript.TRUE)
        {
            if (objectInShape == Defender.LoginCreature || objectInShape == Target) continue;

            int isEnemy = NWScript.GetIsEnemy(objectInShape, Defender.LoginCreature);

            if (isEnemy == NWScript.TRUE)
            {
                bool failed = NWScript.WillSave(objectInShape, difficulty, NWScript.SAVING_THROW_TYPE_LAW) == 0;
                
                if (failed)
                {
                    NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, stunEffect, objectInShape, stunDur);
                }
            }

            objectInShape = NWScript.GetNextObjectInShape(NWScript.SHAPE_SPHERE, NWScript.RADIUS_SIZE_LARGE, NWScript.FALSE,
                NWScript.OBJECT_TYPE_CREATURE);
        }
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