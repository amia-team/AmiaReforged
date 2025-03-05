using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Defender;

[ServiceBinding(typeof(DefendersDutyFactory))]
public class DefendersDutyFactory
{
    
    private readonly SchedulerService _scheduler;
    public DefendersDutyFactory(SchedulerService scheduler)
    {
        _scheduler = scheduler;
    }
    
    
    public DefendersDuty CreateDefendersDuty(NwPlayer defender, NwCreature target)
    {
        return new(defender, target, _scheduler);
    }
}