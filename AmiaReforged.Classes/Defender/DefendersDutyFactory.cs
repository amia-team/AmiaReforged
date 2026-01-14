using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Defender;

[ServiceBinding(typeof(DefendersDutyFactory))]
public class DefendersDutyFactory
{
    private readonly ScriptHandleFactory _scriptHandleFactory;

    public DefendersDutyFactory(ScriptHandleFactory scriptHandleFactory)
    {
        _scriptHandleFactory = scriptHandleFactory;
    }

    public DefendersDuty CreateDefendersDuty(NwPlayer defender) 
        => new(defender, _scriptHandleFactory);
}