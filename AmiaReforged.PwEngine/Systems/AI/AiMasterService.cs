using AmiaReforged.PwEngine.Systems.AI.PackageDefinitions;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.AI;

// [ServiceBinding(typeof(AiMasterService))]
public class AiMasterService
{
    private readonly Dictionary<string, IOnBlockedBehavior> _blockedHandlers = new();
    private readonly Dictionary<string, IOnCombatRoundEndBehavior> _combatRoundEndHandlers = new();
    private readonly Dictionary<string, IOnConversationBehavior> _conversationHandlers = new();
    private readonly Dictionary<string, IOnDamagedBehavior> _damagedHandlers = new();
    private readonly Dictionary<string, IOnDeathBehavior> _deathHandlers = new();
    private readonly Dictionary<string, IOnDisturbedBehavior> _disturbedHandlers = new();
    private readonly Dictionary<string, IOnHeartbeatBehavior> _heartbeatHandlers = new();
    private readonly Dictionary<string, IOnPerceptionBehavior> _perceptionHandlers = new();
    private readonly Dictionary<string, IOnPhysicalAttackedBehavior> _physicalAttackedHandlers = new();
    private readonly Dictionary<string, IOnRestedBehavior> _restedHandlers = new();
    private readonly Dictionary<string, IOnSpawnBehavior> _spawnHandlers = new();
    private readonly Dictionary<string, IOnSpellCastAtBehavior> _spellCastHandlers = new();
    private readonly Dictionary<string, IOnUserDefined> _userDefinedHandlers = new();

    public AiMasterService(ScriptHandleFactory scriptHandleFactory, IEnumerable<IOnBlockedBehavior> onBlockBehaviors,
        IEnumerable<IOnCombatRoundEndBehavior> onCombatRoundEndBehaviors,
        IEnumerable<IOnConversationBehavior> onConversationBehaviors,
        IEnumerable<IOnDamagedBehavior> onDamagedBehaviors,
        IEnumerable<IOnDeathBehavior> onDeathBehaviors,
        IEnumerable<IOnDisturbedBehavior> onDisturbedBehaviors,
        IEnumerable<IOnHeartbeatBehavior> onHeartbeatBehaviors,
        IEnumerable<IOnPerceptionBehavior> onPerceptionBehaviors,
        IEnumerable<IOnPhysicalAttackedBehavior> onPhysicalAttackedBehaviors,
        IEnumerable<IOnRestedBehavior> onRestedBehaviors,
        IEnumerable<IOnSpawnBehavior> onSpawnBehaviors,
        IEnumerable<IOnSpellCastAtBehavior> onSpellCastBehaviors,
        IEnumerable<IOnUserDefined> onUserDefinedBehaviors)
    {
        foreach (IOnBlockedBehavior package in onBlockBehaviors)
        {
            _blockedHandlers.Add(package.ScriptName, package);
            scriptHandleFactory.RegisterScriptHandler(package.ScriptName, HandleOnBlocked);
        }

        foreach (IOnCombatRoundEndBehavior package in onCombatRoundEndBehaviors)
        {
            _combatRoundEndHandlers.Add(package.ScriptName, package);
            scriptHandleFactory.RegisterScriptHandler(package.ScriptName, HandleOnCombatRoundEnd);
        }

        foreach (IOnConversationBehavior package in onConversationBehaviors)
        {
            _conversationHandlers.Add(package.ScriptName, package);
            scriptHandleFactory.RegisterScriptHandler(package.ScriptName, HandleOnConversation);
        }

        foreach (IOnDamagedBehavior package in onDamagedBehaviors)
        {
            _damagedHandlers.Add(package.ScriptName, package);
            scriptHandleFactory.RegisterScriptHandler(package.ScriptName, HandleOnDamaged);
        }

        foreach (IOnDeathBehavior package in onDeathBehaviors)
        {
            _deathHandlers.Add(package.ScriptName, package);
            scriptHandleFactory.RegisterScriptHandler(package.ScriptName, HandleOnDeath);
        }

        foreach (IOnDisturbedBehavior package in onDisturbedBehaviors)
        {
            _disturbedHandlers.Add(package.ScriptName, package);
            scriptHandleFactory.RegisterScriptHandler(package.ScriptName, HandleOnDisturbed);
        }

        foreach (IOnHeartbeatBehavior package in onHeartbeatBehaviors)
        {
            _heartbeatHandlers.Add(package.ScriptName, package);
            scriptHandleFactory.RegisterScriptHandler(package.ScriptName, HandleOnHeartbeat);
        }

        foreach (IOnPerceptionBehavior package in onPerceptionBehaviors)
        {
            _perceptionHandlers.Add(package.ScriptName, package);
            scriptHandleFactory.RegisterScriptHandler(package.ScriptName, HandleOnPerception);
        }

        foreach (IOnPhysicalAttackedBehavior package in onPhysicalAttackedBehaviors)
        {
            _physicalAttackedHandlers.Add(package.ScriptName, package);
            scriptHandleFactory.RegisterScriptHandler(package.ScriptName, HandleOnPhysicalAttacked);
        }

        foreach (IOnRestedBehavior package in onRestedBehaviors)
        {
            _restedHandlers.Add(package.ScriptName, package);
            scriptHandleFactory.RegisterScriptHandler(package.ScriptName, HandleOnRested);
        }

        foreach (IOnSpawnBehavior package in onSpawnBehaviors)
        {
            _spawnHandlers.Add(package.ScriptName, package);
            scriptHandleFactory.RegisterScriptHandler(package.ScriptName, HandleOnSpawn);
        }

        foreach (IOnSpellCastAtBehavior package in onSpellCastBehaviors)
        {
            _spellCastHandlers.Add(package.ScriptName, package);
            scriptHandleFactory.RegisterScriptHandler(package.ScriptName, HandleOnSpellCastAt);
        }

        foreach (IOnUserDefined package in onUserDefinedBehaviors)
        {
            _userDefinedHandlers.Add(package.ScriptName, package);
            scriptHandleFactory.RegisterScriptHandler(package.ScriptName, HandleOnUserDefined);
        }
    }

    private ScriptHandleResult HandleOnDisturbed(CallInfo arg)
    {
        if (!_disturbedHandlers.TryGetValue(arg.ScriptName, out IOnDisturbedBehavior? handler))
            return ScriptHandleResult.NotHandled;

        CreatureEvents.OnDisturbed eventData = new();
        handler.OnDisturbed(eventData);

        return ScriptHandleResult.Handled;
    }

    private ScriptHandleResult HandleOnDeath(CallInfo arg)
    {
        if (!_deathHandlers.TryGetValue(arg.ScriptName, out IOnDeathBehavior? handler))
            return ScriptHandleResult.NotHandled;

        CreatureEvents.OnDeath eventData = new();
        handler.OnDeath(eventData);

        return ScriptHandleResult.Handled;
    }

    private ScriptHandleResult HandleOnDamaged(CallInfo arg)
    {
        if (!_damagedHandlers.TryGetValue(arg.ScriptName, out IOnDamagedBehavior? handler))
            return ScriptHandleResult.NotHandled;

        CreatureEvents.OnDamaged eventData = new();
        handler.OnDamaged(eventData);

        return ScriptHandleResult.Handled;
    }

    private ScriptHandleResult HandleOnHeartbeat(CallInfo arg)
    {
        if (!_heartbeatHandlers.TryGetValue(arg.ScriptName, out IOnHeartbeatBehavior? handler))
            return ScriptHandleResult.NotHandled;

        CreatureEvents.OnHeartbeat eventData = new();
        handler.OnHeartbeat(eventData);

        return ScriptHandleResult.Handled;
    }

    private ScriptHandleResult HandleOnConversation(CallInfo arg)
    {
        if (!_conversationHandlers.TryGetValue(arg.ScriptName, out IOnConversationBehavior? handler))
            return ScriptHandleResult.NotHandled;

        CreatureEvents.OnConversation eventData = new();
        handler.OnConversation(eventData);

        return ScriptHandleResult.Handled;
    }

    private ScriptHandleResult HandleOnCombatRoundEnd(CallInfo arg)
    {
        if (!_combatRoundEndHandlers.TryGetValue(arg.ScriptName, out IOnCombatRoundEndBehavior? handler))
            return ScriptHandleResult.NotHandled;

        CreatureEvents.OnCombatRoundEnd eventData = new();
        handler.OnCombatRoundEnd(eventData);

        return ScriptHandleResult.Handled;
    }

    private ScriptHandleResult HandleOnPerception(CallInfo arg)
    {
        if (!_perceptionHandlers.TryGetValue(arg.ScriptName, out IOnPerceptionBehavior? handler))
            return ScriptHandleResult.NotHandled;

        CreatureEvents.OnPerception eventData = new();
        handler.OnPerception(eventData);

        return ScriptHandleResult.Handled;
    }

    private ScriptHandleResult HandleOnPhysicalAttacked(CallInfo arg)
    {
        if (!_physicalAttackedHandlers.TryGetValue(arg.ScriptName, out IOnPhysicalAttackedBehavior? handler))
            return ScriptHandleResult.NotHandled;

        CreatureEvents.OnPhysicalAttacked eventData = new();
        handler.OnPhysicalAttacked(eventData);

        return ScriptHandleResult.Handled;
    }

    private ScriptHandleResult HandleOnRested(CallInfo arg)
    {
        if (!_restedHandlers.TryGetValue(arg.ScriptName, out IOnRestedBehavior? handler))
            return ScriptHandleResult.NotHandled;

        CreatureEvents.OnRested eventData = new();
        handler.OnRested(eventData);

        return ScriptHandleResult.Handled;
    }

    private ScriptHandleResult HandleOnSpawn(CallInfo arg)
    {
        if (!_spawnHandlers.TryGetValue(arg.ScriptName, out IOnSpawnBehavior? handler))
            return ScriptHandleResult.NotHandled;

        CreatureEvents.OnSpawn eventData = new();
        handler.OnSpawn(eventData);

        return ScriptHandleResult.Handled;
    }

    private ScriptHandleResult HandleOnSpellCastAt(CallInfo arg)
    {
        if (!_spellCastHandlers.TryGetValue(arg.ScriptName, out IOnSpellCastAtBehavior? handler))
            return ScriptHandleResult.NotHandled;

        CreatureEvents.OnSpellCastAt eventData = new();
        handler.OnSpellCastAt(eventData);

        return ScriptHandleResult.Handled;
    }

    private ScriptHandleResult HandleOnUserDefined(CallInfo arg)
    {
        if (!_userDefinedHandlers.TryGetValue(arg.ScriptName, out IOnUserDefined? handler))
            return ScriptHandleResult.NotHandled;

        CreatureEvents.OnUserDefined eventData = new();
        handler.UserDefined(eventData);

        return ScriptHandleResult.Handled;
    }

    private ScriptHandleResult HandleOnBlocked(CallInfo arg)
    {
        if (!_blockedHandlers.TryGetValue(arg.ScriptName, out IOnBlockedBehavior? handler))
            return ScriptHandleResult.NotHandled;

        CreatureEvents.OnBlocked eventData = new();
        handler.OnBlocked(eventData);

        return ScriptHandleResult.Handled;
    }
}