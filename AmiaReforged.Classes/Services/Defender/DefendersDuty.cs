using Anvil.API;
using Anvil.API.Events;
using NLog.Targets;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Services.Defender;

public class DefendersDuty
{
    private const float DefenderDamage = 0.25f;

    public NwPlayer Defender { get; private set; }
    public NwCreature Target { get; private set; }

    public DefendersDuty(NwPlayer defender, NwCreature target)
    {
        Defender = defender;
        Target = target;
    }

    public void Apply()
    {
        const float duration = 7.0f;

        Target.OnCreatureDamage += SoakDamage;

        Defender.LoginCreature?.JumpToObject(Target);
        Defender.LoginCreature?.SpeakString("*jumps to protecc fren :)))))*");

        NWScript.DelayCommand(duration, () => { Target.OnCreatureDamage -= SoakDamage; });
    }

    public void Stop()
    {
        Target.OnCreatureDamage -= SoakDamage;
    }
    private void SoakDamage(OnCreatureDamage obj)
    {
        DamageData defenderDamageData = new DamageData()
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