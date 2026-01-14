using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Defender;

[ServiceBinding(typeof(DefendersDutyFactory))]
public class DefendersDutyFactory
{
    private readonly SchedulerService _scheduler;
    private readonly ScriptHandleFactory _scriptHandleFactory;

    public DefendersDutyFactory(SchedulerService scheduler, ScriptHandleFactory scriptHandleFactory)
    {
        _scheduler = scheduler;
        _scriptHandleFactory = scriptHandleFactory;
    }


    public DefendersDuty CreateDefendersDuty(NwPlayer defender, NwCreature target) 
        => new(defender, target, _scheduler, _scriptHandleFactory);
}