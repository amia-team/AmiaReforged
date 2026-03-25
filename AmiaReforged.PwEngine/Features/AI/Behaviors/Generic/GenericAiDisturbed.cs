using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors.Generic;

/// <summary>
/// Generic AI disturbed handler.
/// Ports ds_ai2_disturb.nss — currently a no-op placeholder matching legacy behavior.
/// The interface is implemented so AiMasterService can register the script handler.
/// </summary>
[ServiceBinding(typeof(IOnDisturbedBehavior))]
public class GenericAiDisturbed : IOnDisturbedBehavior
{
    private readonly bool _isEnabled;

    public string ScriptName => "ds_ai_disturb";

    public GenericAiDisturbed()
    {
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public void OnDisturbed(CreatureEvents.OnDisturbed eventData)
    {
        if (!_isEnabled) return;

        // Legacy ds_ai2_disturb.nss: "doesn't do anything yet"
        // Placeholder for future inventory theft / pickpocket response
    }
}
