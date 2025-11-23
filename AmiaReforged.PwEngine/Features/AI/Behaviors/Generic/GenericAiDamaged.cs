using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Core.Services;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors.Generic;

/// <summary>
/// Generic AI damaged handler that manages target switching and flee behavior.
/// Ports logic from ds_ai_damaged.nss.
/// </summary>
[ServiceBinding(typeof(IOnDamagedBehavior))]
public class GenericAiDamaged : IOnDamagedBehavior
{
    private readonly AiStateManager _stateManager;
    private readonly AiArchetypeService _archetypeService;
    private readonly bool _isEnabled;

    // BREAKCOMBAT constant from ds_ai_include.nss (chance to switch targets)
    private const int BreakCombat = 50;

    public string ScriptName => "ds_ai_damaged";

    public GenericAiDamaged(
        AiStateManager stateManager,
        AiArchetypeService archetypeService)
    {
        _stateManager = stateManager;
        _archetypeService = archetypeService;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public void OnDamaged(CreatureEvents.OnDamaged eventData)
    {
        if (!_isEnabled) return;

        var creature = eventData.Creature;

        // Skip player-controlled creatures
        if (creature.IsPlayerControlled || creature.IsDMAvatar) return;

        var damager = eventData.Damager;
        if (damager is not NwCreature damagerCreature) return;

        var state = _stateManager.GetState(creature);
        if (state == null) return;

        // Track last damager
        state.LastDamager = damagerCreature;

        // Detect archetype value if not already set
        int archetypeValue = 5; // Default to hybrid
        if (!string.IsNullOrEmpty(state.ArchetypeId))
        {
            archetypeValue = state.ArchetypeId switch
            {
                "melee" => 2,
                "hybrid" => 5,
                "caster" => 8,
                _ => 5
            };
        }

        // Casters (high archetype value) flee when damaged at close range
        // (d10() + 2) < nArchetype means higher archetype = more likely to flee
        int diceRoll = Random.Shared.Next(1, 11) + 2; // d10 + 2

        if (diceRoll < archetypeValue)
        {
            float distance = creature.Distance(damagerCreature);

            if (distance < 5.0f)
            {
                // Casters flee from close combat
                state.CurrentTarget = damagerCreature;
            }
        }
        else
        {
            // Melees/hybrids may switch targets
            if (state.CurrentTarget != damagerCreature)
            {
                // Check if we can see the damager
                if (NWScript.GetObjectSeen(damagerCreature, creature) == 1)
                {
                    // Random chance to break combat and switch targets
                    int breakRoll = Random.Shared.Next(1, 101) - 20; // d100 - 20

                    if (breakRoll < BreakCombat)
                    {
                        state.CurrentTarget = damagerCreature;
                    }
                }
            }
        }
    }
}
