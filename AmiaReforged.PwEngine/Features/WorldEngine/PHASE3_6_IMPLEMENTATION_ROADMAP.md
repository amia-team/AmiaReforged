# Phase 3.6: Resource Node Provisioning - Implementation Roadmap

## Status: Implementation Plan
**Date**: October 28, 2025

---

## 🎯 Phased Approach

This document breaks down the complete Phase 3.6 vision into three implementable phases:
- **Near Term**: Get basic trigger-based spawning working (MVP)
- **Mid Term**: Add player discovery mechanics
- **Long Term**: Enable NPC competition system

---

## 📍 NEAR TERM: Trigger-Based Spawning (MVP)

### Goal
Get resources spawning inside triggers with intelligent distribution and slot limits.

### Success Criteria
- [x] Nodes spawn only within designer-placed triggers
- [x] Up to 5 total nodes per trigger (hard cap)
- [x] At least 1 of each node type from `node_tags` spawns (if space allows)
- [x] Reasonable spacing between nodes (minimum 5-10 meters)
- [x] Random rotations for each node
- [x] Walkability validation

### Implementation Tasks

#### 1. Create TriggerBasedSpawnService
**File**: `Features/WorldEngine/ResourceNodes/Services/TriggerBasedSpawnService.cs`

**Responsibilities:**
- Find all triggers tagged `worldengine_node_region`
- Parse `node_tags` CSV from local variables
- Generate spawn locations within trigger geometry
- Ensure minimum spacing between nodes
- Validate walkability

**Key Methods:**
```csharp
public class TriggerBasedSpawnService
{
    // Find all resource zone triggers in an area
    List<NwTrigger> GetResourceTriggers(NwArea area);

    // Generate up to maxNodes spawn locations in a trigger
    // Guarantees at least 1 of each type if possible
    List<SpawnLocation> GenerateSpawnLocations(
        NwTrigger trigger,
        List<string> nodeTags,
        int maxNodes = 5
    );

    // Generate random point inside trigger polygon
    Vector3 GetRandomPointInTrigger(NwTrigger trigger);

    // Check if position is valid (walkable, not too close to others)
    bool IsValidSpawnPosition(
        NwArea area,
        Vector3 position,
        List<Vector3> existingPositions,
        float minDistance = 7.5f
    );

    // Distribute node types fairly across slots
    List<string> DistributeNodeTypes(
        List<string> nodeTags,
        int totalSlots
    );
}
```

**Algorithm for Fair Distribution:**
```csharp
// Example: node_tags = "ore_copper,ore_iron,tree_oak", maxNodes = 5
// 1. Ensure at least 1 of each type: ["ore_copper", "ore_iron", "tree_oak"]
// 2. Remaining slots (5-3=2): distribute evenly or randomly
// 3. Final distribution: ["ore_copper", "ore_iron", "tree_oak", "ore_copper", "tree_oak"]
```

#### 2. Update ProvisionAreaNodesCommandHandler
**File**: `Features/WorldEngine/ResourceNodes/Application/ProvisionAreaNodesCommandHandler.cs`

**Changes:**
```csharp
public async Task<CommandResult> HandleAsync(ProvisionAreaNodesCommand command)
{
    var area = command.AreaDefinition;
    var nwArea = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == area.ResRef.Value);

    if (nwArea == null)
        return CommandResult.Fail($"Area {area.ResRef} not found");

    var provisionedNodes = new List<ResourceNodeInstance>();

    // Find all resource zone triggers in this area
    var triggers = _triggerSpawnService.GetResourceTriggers(nwArea);

    foreach (var trigger in triggers)
    {
        // Get node tags from trigger
        var nodeTagsCsv = trigger.GetLocalVariable<string>(WorldConstants.LvarNodeTags).Value;
        if (string.IsNullOrEmpty(nodeTagsCsv)) continue;

        var nodeTags = nodeTagsCsv.Split(',').Select(t => t.Trim()).ToList();

        // Get max nodes (default 5)
        var maxNodes = trigger.GetLocalVariable<int>(WorldConstants.LvarMaxNodesTotal).Value;
        if (maxNodes <= 0) maxNodes = 5;

        // Generate spawn locations
        var spawnLocations = _triggerSpawnService.GenerateSpawnLocations(
            trigger,
            nodeTags,
            maxNodes
        );

        // Create and spawn nodes at each location
        foreach (var location in spawnLocations)
        {
            var nodeDefinition = _definitionRepository.Get(location.NodeTag);
            if (nodeDefinition == null) continue;

            var node = _nodeService.CreateNewNode(
                area,
                nodeDefinition,
                location.Position,
                location.Rotation
            );

            node.Metadata["triggerSource"] = trigger.Tag;

            _nodeService.SpawnInstance(node);
            provisionedNodes.Add(node);
        }
    }

    // Publish event...
    return CommandResult.Ok(/*...*/);
}
```

#### 3. Add WorldConstants
**File**: `Features/WorldEngine/WorldConstants.cs` (or create if doesn't exist)

```csharp
public static class WorldConstants
{
    // Resource Node Triggers
    public const string ResourceNodeZoneTag = "worldengine_node_region";
    public const string LvarNodeTags = "node_tags"; // CSV
    public const string LvarMaxNodesTotal = "max_nodes_total"; // Default: 5

    // Spawn Configuration
    public const int DefaultMaxNodesPerTrigger = 5;
    public const float MinNodeSpacing = 7.5f; // meters
}
```

#### 4. Update SpawnLocation Value Object
**File**: `Features/WorldEngine/ResourceNodes/Services/SpawnLocation.cs`

```csharp
public class SpawnLocation
{
    public Vector3 Position { get; init; }
    public float Rotation { get; init; }
    public string NodeTag { get; init; } // Which node type to spawn
    public string? TriggerSource { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}
```

### Testing Plan
- [ ] Create test area with 2-3 triggers
- [ ] Trigger 1: `node_tags = "ore_copper,ore_iron"`, max 5 nodes
  - Expected: At least 1 copper, 1 iron, up to 3 more distributed
- [ ] Trigger 2: `node_tags = "tree_oak,tree_pine,herb_ginseng"`, max 5 nodes
  - Expected: At least 1 of each type, up to 2 more distributed
- [ ] Verify minimum spacing (no nodes within 7.5m of each other)
- [ ] Verify random rotations (not all facing same direction)
- [ ] Verify all positions are walkable

### Deliverables
- ✅ Trigger-based spawning service
- ✅ Updated provisioning command handler
- ✅ WorldConstants for configuration
- ✅ Working spawn distribution algorithm
- ✅ Minimum spacing validation

---

## 🔍 MID TERM: Discovery System

### Goal
Allow players to discover new nodes based on knowledge, with configurable difficulty.

### Success Criteria
- [x] Players can manually discover nodes when in resource zones
- [x] Discovery difficulty configurable per node type
- [x] Knowledge system integration (specific knowledge aids specific discoveries)
- [x] Node slots still respected (up to 5 per trigger)
- [x] Discovery metadata persisted

### New Data Structures

#### 1. Update ResourceNodeDefinition
**File**: `Features/WorldEngine/ResourceNodes/ResourceNodeData/ResourceNodeDefinition.cs`

**Add Discovery Configuration:**
```csharp
public class ResourceNodeDefinition
{
    // ...existing properties...

    public DiscoveryConfig Discovery { get; set; } = new();
}

public class DiscoveryConfig
{
    /// <summary>
    /// How difficult to discover (1-10, higher = harder)
    /// </summary>
    public int Difficulty { get; set; } = 5;

    /// <summary>
    /// Discovery method: "prospecting", "foraging", "woodcraft"
    /// </summary>
    public string Method { get; set; } = "prospecting";

    /// <summary>
    /// Knowledge tags that help discover this node
    /// e.g., ["metallurgy", "geology", "adamant_expertise"]
    /// </summary>
    public List<string> RelevantKnowledge { get; set; } = new();

    /// <summary>
    /// Informational tags for system decision-making
    /// e.g., ["rare", "deep_cave", "volcanic"]
    /// </summary>
    public List<string> InfoTags { get; set; } = new();
}
```

#### 2. Example Node Definition Updates
**File**: `Resources/WorldEngine/Nodes/Ore/node_ore_adamant.json`

```json
{
  "NodeTag": "ore_adamant",
  "Name": "Adamant Ore Vein",
  "Description": "A vein of extremely rare adamant ore.",
  "Type": "ore",
  "Requirement": {
    "RequiredItemType": "Pickaxe",
    "MinimumQuality": "Good"
  },
  "YieldTable": {
    "Yields": [
      {
        "ItemTag": "item_ore_adamant",
        "MinQuantity": 1,
        "MaxQuantity": 3
      }
    ]
  },
  "Discovery": {
    "Difficulty": 9,
    "Method": "prospecting",
    "RelevantKnowledge": [
      "expert_metallurgist",
      "master_geologist",
      "adamant_expertise"
    ],
    "InfoTags": [
      "rare",
      "deep_underground",
      "requires_expertise"
    ]
  }
}
```

**File**: `Resources/WorldEngine/Nodes/Trees/node_tree_oak.json`

```json
{
  "NodeTag": "tree_oak",
  "Name": "Oak Tree",
  "Description": "A sturdy oak tree.",
  "Type": "tree",
  "Requirement": {
    "RequiredItemType": "Axe",
    "MinimumQuality": "Average"
  },
  "YieldTable": {
    "Yields": [
      {
        "ItemTag": "item_log_oak",
        "MinQuantity": 2,
        "MaxQuantity": 5
      }
    ]
  },
  "Discovery": {
    "Difficulty": 3,
    "Method": "woodcraft",
    "RelevantKnowledge": [
      "master_forester",
      "ranger_training",
      "woodland_lore"
    ],
    "InfoTags": [
      "common",
      "temperate_forest"
    ]
  }
}
```

### Implementation Tasks

#### 1. Create Discovery Command & Handler
**File**: `Features/WorldEngine/ResourceNodes/Commands/DiscoverResourceCommand.cs`

```csharp
public class DiscoverResourceCommand : ICommand
{
    public NwPlayer Player { get; }
    public NwTrigger Trigger { get; }
    public string DiscoveryMethod { get; } // "prospecting", "foraging", "woodcraft"

    public DiscoverResourceCommand(NwPlayer player, NwTrigger trigger, string method)
    {
        Player = player;
        Trigger = trigger;
        DiscoveryMethod = method;
    }
}
```

**File**: `Features/WorldEngine/ResourceNodes/Application/DiscoverResourceCommandHandler.cs`

```csharp
[ServiceBinding(typeof(ICommandHandler<DiscoverResourceCommand>))]
public class DiscoverResourceCommandHandler : ICommandHandler<DiscoverResourceCommand>
{
    public async Task<CommandResult> HandleAsync(DiscoverResourceCommand command)
    {
        // 1. Check node slots in trigger (up to 5 total)
        var existingNodes = GetNodesInTrigger(command.Trigger);
        if (existingNodes.Count >= 5)
            return CommandResult.Fail("This area is already rich in resources.");

        // 2. Get available node types from trigger's node_tags
        var availableNodeTags = GetAvailableNodeTags(command.Trigger, command.DiscoveryMethod);
        if (!availableNodeTags.Any())
            return CommandResult.Fail("No resources of this type can be found here.");

        // 3. Get player's relevant knowledge from Codex
        var playerKnowledge = await GetPlayerKnowledge(command.Player);

        // 4. Calculate success chance for each available node type
        var bestMatch = CalculateBestDiscoveryMatch(
            availableNodeTags,
            playerKnowledge
        );

        if (bestMatch == null)
            return CommandResult.Fail("You lack the expertise to find resources here.");

        // 5. Roll for success
        if (!RollDiscoverySuccess(bestMatch.SuccessChance))
            return CommandResult.Fail("Your search yields nothing.");

        // 6. Generate spawn location in trigger
        var location = _spawnService.GenerateSpawnLocation(
            command.Trigger,
            existingNodes.Select(n => n.Position).ToList()
        );

        // 7. Create and spawn node
        var node = CreateDiscoveredNode(
            command.Player,
            bestMatch.NodeDefinition,
            location,
            command.Trigger
        );

        // 8. Grant knowledge XP
        await GrantKnowledgeXP(command.Player, bestMatch.UsedKnowledge);

        return CommandResult.Ok(/*...*/);
    }

    private DiscoveryMatch? CalculateBestDiscoveryMatch(
        List<ResourceNodeDefinition> availableNodes,
        List<string> playerKnowledge)
    {
        var matches = new List<DiscoveryMatch>();

        foreach (var nodeDef in availableNodes)
        {
            var relevantKnowledge = nodeDef.Discovery.RelevantKnowledge
                .Intersect(playerKnowledge)
                .ToList();

            if (!relevantKnowledge.Any())
                continue; // Player doesn't have required knowledge

            // Calculate success chance
            // Base: 10%
            // Per relevant knowledge: +15%
            // Difficulty penalty: -5% per difficulty point above 5
            var baseChance = 10f;
            var knowledgeBonus = relevantKnowledge.Count * 15f;
            var difficultyPenalty = Math.Max(0, nodeDef.Discovery.Difficulty - 5) * 5f;

            var finalChance = Math.Clamp(
                baseChance + knowledgeBonus - difficultyPenalty,
                5f,  // Minimum 5%
                90f  // Maximum 90%
            );

            matches.Add(new DiscoveryMatch
            {
                NodeDefinition = nodeDef,
                SuccessChance = finalChance,
                UsedKnowledge = relevantKnowledge
            });
        }

        // Return best match (highest success chance)
        return matches.OrderByDescending(m => m.SuccessChance).FirstOrDefault();
    }
}

public class DiscoveryMatch
{
    public ResourceNodeDefinition NodeDefinition { get; set; }
    public float SuccessChance { get; set; }
    public List<string> UsedKnowledge { get; set; }
}
```

#### 2. Create ResourceZoneBehavior
**File**: `Features/WorldEngine/ResourceNodes/Services/ResourceZoneBehavior.cs`

```csharp
[ServiceBinding(typeof(ResourceZoneBehavior))]
public class ResourceZoneBehavior
{
    private readonly Dictionary<uint, NwTrigger> _playerCurrentZones = new();

    public ResourceZoneBehavior()
    {
        foreach (var area in NwModule.Instance.Areas)
        {
            var triggers = area.Objects.OfType<NwTrigger>()
                .Where(t => t.Tag == WorldConstants.ResourceNodeZoneTag);

            foreach (var trigger in triggers)
            {
                trigger.OnEnter += OnPlayerEnterZone;
                trigger.OnExit += OnPlayerExitZone;
            }
        }
    }

    private void OnPlayerEnterZone(TriggerEvents.OnEnter e)
    {
        if (!e.EnteringObject.IsPlayerControlled(out var player))
            return;

        var trigger = (NwTrigger)e.TriggeringSelf;
        _playerCurrentZones[player.ObjectId] = trigger;

        // Enable discovery actions based on node_tags
        EnableDiscoveryActions(player, trigger);
    }

    private void EnableDiscoveryActions(NwPlayer player, NwTrigger trigger)
    {
        var nodeTags = trigger.GetLocalVariable<string>(WorldConstants.LvarNodeTags).Value;
        // Parse and enable appropriate discovery actions
        // This hooks into your action system (context menu, radial menu, etc.)
    }

    public NwTrigger? GetPlayerCurrentZone(NwPlayer player)
    {
        return _playerCurrentZones.TryGetValue(player.ObjectId, out var trigger)
            ? trigger
            : null;
    }
}
```

### Testing Plan
- [ ] Update node JSON files with discovery configurations
- [ ] Create test character with specific knowledge
- [ ] Test discovery with matching knowledge (high success rate)
- [ ] Test discovery without knowledge (failure)
- [ ] Test discovery difficulty scaling
- [ ] Verify node slot limits still enforced
- [ ] Verify metadata persisted (discoverer, timestamp, knowledge used)

### Deliverables
- ✅ Updated node definitions with discovery configs
- ✅ Discovery command and handler
- ✅ ResourceZoneBehavior for trigger tracking
- ✅ Knowledge-based success calculation
- ✅ Integration with Codex system

---

## 🤖 LONG TERM: NPC Activity System

### Goal
Create extensible foundation for NPC prospectors to compete with players.

### Design Principles
- **Extensible Architecture**: NPC behavior pluggable without changing core systems
- **Shared Command Path**: NPCs use same `DiscoverResourceCommand` as players
- **AI Decision Framework**: NPCs make "intelligent" decisions about where/when to prospect

### Key Extension Points

#### 1. Update DiscoverResourceCommand
**Allow NPC Actors:**
```csharp
public class DiscoverResourceCommand : ICommand
{
    public NwCreature Discoverer { get; } // Can be NwPlayer or NwCreature
    public NwTrigger Trigger { get; }
    public string DiscoveryMethod { get; }
    public bool IsNPC { get; }

    // Constructor for players
    public DiscoverResourceCommand(NwPlayer player, NwTrigger trigger, string method)
    {
        Discoverer = player.ControlledCreature;
        Trigger = trigger;
        DiscoveryMethod = method;
        IsNPC = false;
    }

    // Constructor for NPCs
    public DiscoverResourceCommand(NwCreature npc, NwTrigger trigger, string method)
    {
        Discoverer = npc;
        Trigger = trigger;
        DiscoveryMethod = method;
        IsNPC = true;
    }
}
```

#### 2. Create NPCProspectingService Interface
**File**: `Features/WorldEngine/ResourceNodes/Services/INPCProspectingService.cs`

```csharp
public interface INPCProspectingService
{
    /// <summary>
    /// Check if NPCs should prospect in any zones
    /// Called periodically by background service
    /// </summary>
    Task CheckAndTriggerNPCProspecting();

    /// <summary>
    /// Calculate probability of NPC prospecting in a zone
    /// </summary>
    float CalculateNPCProspectingChance(
        NwTrigger zone,
        TimeSpan timeSinceLastPlayerDiscovery,
        int availableSlots
    );

    /// <summary>
    /// Spawn NPC prospector at discovered node
    /// </summary>
    Task<NwCreature?> SpawnNPCProspector(
        ResourceNodeInstance node,
        NwTrigger zone
    );
}
```

#### 3. Stub Implementation (For Now)
**File**: `Features/WorldEngine/ResourceNodes/Services/NPCProspectingService.cs`

```csharp
[ServiceBinding(typeof(INPCProspectingService))]
public class NPCProspectingService : INPCProspectingService
{
    public Task CheckAndTriggerNPCProspecting()
    {
        // TODO: Implement in long-term phase
        return Task.CompletedTask;
    }

    public float CalculateNPCProspectingChance(
        NwTrigger zone,
        TimeSpan timeSinceLastPlayerDiscovery,
        int availableSlots)
    {
        // Stub: Always return 0 for now (no NPC prospecting)
        return 0f;
    }

    public Task<NwCreature?> SpawnNPCProspector(
        ResourceNodeInstance node,
        NwTrigger zone)
    {
        // TODO: Implement in long-term phase
        return Task.FromResult<NwCreature?>(null);
    }
}
```

#### 4. Add Tracking Infrastructure
**File**: `Features/WorldEngine/ResourceNodes/Services/ZoneActivityTracker.cs`

```csharp
[ServiceBinding(typeof(ZoneActivityTracker))]
public class ZoneActivityTracker
{
    private readonly Dictionary<string, DateTime> _lastPlayerDiscovery = new();

    public void RecordPlayerDiscovery(NwTrigger zone)
    {
        _lastPlayerDiscovery[zone.UUID.ToString()] = DateTime.UtcNow;
    }

    public TimeSpan GetTimeSinceLastPlayerDiscovery(NwTrigger zone)
    {
        var key = zone.UUID.ToString();
        if (!_lastPlayerDiscovery.ContainsKey(key))
            return TimeSpan.MaxValue; // Never discovered

        return DateTime.UtcNow - _lastPlayerDiscovery[key];
    }

    public List<NwTrigger> GetLowActivityZones(TimeSpan threshold)
    {
        // Return zones with no player activity in X hours
        // To be used by NPC prospecting service
        return new List<NwTrigger>(); // TODO: Implement
    }
}
```

### Future Implementation Notes
When implementing NPC prospecting in the future:
1. Create NPC prospector blueprints/archetypes
2. Implement patrol/travel AI to resource zones
3. Add loot tables for defeated NPC prospectors
4. Create economy integration (NPC-harvested resources → market)
5. Add faction/guild affiliations for NPCs
6. Implement NPC mining camps at productive sites

### Deliverables
- ✅ Extension points in command structure
- ✅ Stub NPC prospecting service
- ✅ Zone activity tracking infrastructure
- ✅ Documentation of future implementation path

---

## 📦 Summary

### Implementation Order
1. **Week 1-2**: Near Term (Trigger-based spawning)
2. **Week 3-4**: Mid Term (Discovery system)
3. **Future**: Long Term (NPC activity) - when ready

### Dependencies
- **Near Term**: None (can start immediately)
- **Mid Term**: Requires Near Term complete + Codex knowledge system
- **Long Term**: Requires Mid Term complete + NPC AI framework

### Key Files to Create
**Near Term:**
- `TriggerBasedSpawnService.cs`
- `WorldConstants.cs` (or update existing)

**Mid Term:**
- `DiscoverResourceCommand.cs`
- `DiscoverResourceCommandHandler.cs`
- `ResourceZoneBehavior.cs`
- Update all node JSON files with discovery configs

**Long Term:**
- `INPCProspectingService.cs`
- `NPCProspectingService.cs` (stub)
- `ZoneActivityTracker.cs`

### Success Metrics
**Near Term:**
- [ ] Nodes spawn in triggers only
- [ ] Max 5 nodes per trigger enforced
- [ ] Fair distribution across node types
- [ ] Proper spacing and rotation

**Mid Term:**
- [ ] Discovery actions available in zones
- [ ] Knowledge affects success rates
- [ ] Difficulty scaling works correctly
- [ ] Discovery metadata persisted

**Long Term:**
- [ ] Architecture supports NPC actors
- [ ] Activity tracking functional
- [ ] Stub service in place for future work

---

*This phased approach allows incremental delivery of value while maintaining extensibility for future features.*

