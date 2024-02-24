using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Services;

[ServiceBinding(typeof(WizardSpecializationService))]
public class WizardSpecializationService
{
    
    private readonly Logger Log = LogManager.GetCurrentClassLogger();

    public WizardSpecializationService()
    {
        
        Log.Info("Wizard Specialization Service initialized.");

        NwModule.Instance.OnSpellCast += HandleArcaneSpellCast;
    }

    private void HandleArcaneSpellCast(OnSpellCast obj)
    {
        if (!obj.Caster.IsPlayerControlled(out NwPlayer? player)) return;
        
        if(player.LoginCreature.GetSpecialization() == SpellSchool.Unknown) return;
           
    }
}
