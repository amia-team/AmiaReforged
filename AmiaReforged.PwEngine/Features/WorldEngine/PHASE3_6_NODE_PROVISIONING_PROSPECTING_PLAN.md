# Phase 3.6: Resource Node Provisioning, Prospecting & Persistence

## Status: Planning → Implementation Ready
**Date**: October 28, 2025

## Overview

This phase implements a complete resource node lifecycle system using trigger-based spawn zones, dynamic prospecting mechanics, and persistent state management. The system combines level designer control with dynamic gameplay elements.

---

## 🎯 Goals

1. **Trigger-Based Provisioning** - Use level-designed trigger zones for intelligent node spawning
2. **Dynamic Discovery** - Allow players/NPCs to discover new resource nodes based on knowledge
3. **Persistent State** - Track node states across server restarts
4. **Natural Respawning** - Nodes respawn in different locations within valid zones
5. **Designer-Friendly** - Simple workflow for level designers to add resource zones

---

## 🏗️ Architecture

### Current State ✅

#### Persistence Layer (Already Implemented)
- `PersistentResourceNodeInstance` entity in database
- `ResourceNodeInstanceRepository` for CRUD operations
- `ResourceNodeInstance` aggregate with domain logic
- Event-driven state changes (depletion, harvesting)

#### CQRS Infrastructure (Already Implemented)
- Command/Query separation
- Event bus with marker interface pattern
- Domain events for state changes
- Command handlers with validation

#### Resource Definitions (Already Implemented)
- JSON-based node definitions (`Resources/WorldEngine/Nodes/`)
- Item definitions for harvested resources
- Region definitions with resource associations

### New Components 🆕

#### 1. Trigger-Based Spawn System

```
Level Designer Creates Trigger
    ↓
Tag: WorldConstants.ResourceNodeZoneTag
LocalString "node_tags": "ore,geode,tree_oak"
    ↓
TriggerBasedSpawnService discovers trigger
    ↓
Generates random points within trigger geometry
    ↓
Validates each point (walkable, not water, etc.)
    ↓
Creates ResourceNodeInstance at valid locations
```

**Key Classes:**
- `TriggerBasedSpawnService` - Core spawning logic
- `SpawnLocation` - Value object for spawn point + metadata
- `TriggerGeometryHelper` - Polygon math for random point generation

#### 2. Discovery System (Knowledge-Based, Manual Action)

```
Player Enters Resource Zone Trigger
    ↓
ResourceZoneBehavior tracks player entry
    ↓
Discovery action becomes available to player
    (e.g., "Prospect for Ore", "Forage for Herbs")
    ↓
Player manually uses discovery action
    ↓
DiscoverResourceCommand issued
    ↓
DiscoverResourceCommandHandler validates:
    ↓
├─ Is player still in resource zone trigger?
├─ Does player have relevant knowledge?
└─ Are there available node slots? (< 4 of this type)
    ↓
If all checks pass:
    ↓
Determines success based on knowledge level
    ↓
If successful:
    ↓
Spawns node within current trigger bounds
    ↓
Publishes ResourceDiscoveredEvent
    ↓
Node marked with discoverer metadata
    ↓
Player notified of discovery
    ↓
NPCProspectingService records player activity
```

**NPC Competition System**

```
NPCProspectingService runs periodic check (every 5 minutes)
    ↓
For each resource zone trigger:
    ↓
Check player discovery activity in last X hours
    ↓
If low/no activity detected:
    ↓
Calculate NPC prospecting chance based on:
    ├─ Time since last player discovery
    ├─ Zone danger level (higher = NPCs less likely)
    ├─ Available node slots
    └─ Zone accessibility (remote zones = NPCs more active)
    ↓
Roll for NPC prospecting attempt
    ↓
If successful:
    ↓
DiscoverResourceCommand issued (with NPC actor)
    ↓
Spawn node in zone
    ↓
NPC "claims" the resource (begins harvesting)
    ↓
Publishes ResourceDiscoveredEvent (by NPC)
    ↓
Players can see NPC harvesting (PvE encounter opportunity)
```

**Trigger-Based Availability**: The system only allows discovery actions when a player is physically in a resource zone trigger. The action becomes available/enabled when entering the zone, disabled when leaving.

**Node Slot Limits**: Each trigger zone has a maximum number of active nodes per resource type (default: 4). This prevents over-saturation and ensures nodes remain valuable. Discovery attempts fail if the zone is at capacity for that resource type.

**Activity-Based Competition**: Instead of cooldowns, the system monitors player discovery activity. When players aren't actively discovering resources, NPC prospectors are triggered to search for new nodes. This creates:
- **Dynamic Competition**: Players can prospect whenever they want, but NPCs will compete if they don't
- **Risk/Reward**: Players must enter dangerous areas to prospect before NPCs do
- **Emergent Economy**: NPC-discovered resources enter the market, affecting prices
- **No Arbitrary Timers**: Natural scarcity through competition, not artificial cooldowns

**Purpose**: Provides interactive way to handle resource depletion. When an area's nodes are depleted, players or NPCs with appropriate knowledge can discover new ones, creating a living, competitive economy.

**Key Classes:**
- `ResourceZoneBehavior` - Handles OnTriggerEnter/Exit, tracks player presence
- `DiscoverResourceCommand` - Command with player/NPC, trigger, discovery type
- `DiscoverResourceCommandHandler` - Knowledge checks, slot validation, node creation
- `ResourceDiscoveredEvent` - Domain event for discoveries
- `KnowledgeDiscoveryService` - Integrates with Codex knowledge system
- `NodeSlotTracker` - Tracks active node counts per trigger per type
- `NPCProspectingService` - Monitors player activity, triggers NPC prospectors
- `ProspectorNPC` - AI actor that can discover resources in zones

**Discovery Types:**
- **Prospecting** (Minerals): Requires knowledge like "Expert Metallurgist", "Geologist", "Master Miner"
  - Discovers: ores, geodes, boulders, mineral deposits
- **Foraging** (Herbs): Requires knowledge like "Herbalist", "Botanist", "Druid Lore"
  - Discovers: herbs, fungi, medicinal plants
- **Woodcraft** (Timber): Requires knowledge like "Master Forester", "Carpenter", "Ranger Training"
  - Discovers: special trees, rare woods

**Discovery ≠ Harvesting**: Critical distinction for economic gameplay:
- **Discovery** requires **knowledge** (Codex-based, intellectual expertise)
  - Example: A scholar with "Expert Metallurgist" can identify and locate ore veins
  - But they may lack the **skill** to actually mine them effectively

- **Harvesting** requires **skill** (Harvesting system, physical ability)
  - Example: A miner with high mining skill can extract ore efficiently
  - But they may not know where to find rare ores without guidance

**Economic Opportunities:**
1. **Specialist Roles**:
   - Knowledge specialists (prospectors) discover resources
   - Skill specialists (miners/harvesters) extract them
   - Creates interdependence and trade

2. **Guild Operations**:
   - Guilds employ prospectors to find nodes
   - Guilds employ miners to harvest them
   - Stockpile resources for processing or market sale
   - Control resource supply chains

3. **Market Recovery**:
   - Lost discovery opportunity (NPC found it first)?
   - Buy the resources from the market instead
   - Money can substitute for time/risk
   - Creates viable alternative to dangerous prospecting

4. **Information Economy**:
   - Prospectors can sell "node locations" to harvesters
   - Maps/coordinates become tradeable commodities
   - Knowledge itself has market value

**Example Flow:**
```
Scholar with "Expert Metallurgist" knowledge (but low mining skill)
    ↓
Discovers mithral ore vein in dangerous cave
    ↓
Has two options:
    ├─ Harvest it themselves (slow, dangerous, inefficient)
    └─ Sell location to guild/miner (safe, profitable)
        ↓
Guild sends skilled miner to extract ore
        ↓
Guild stockpiles/processes/sells mithral
        ↓
Scholar who discovered it can buy processed mithral from market
        (if they need it but don't want to risk harvesting)
```

#### 3. Respawn System

```
Node Depleted
    ↓
NodeDepletedEvent published
    ↓
NodeRespawnService schedules respawn
    ↓
After cooldown timer expires
    ↓
Find original trigger zone
    ↓
Generate NEW random location in same trigger
    ↓
Spawn node at new position
    ↓
Update persistence layer
```

**Key Classes:**
- `NodeRespawnService` - Manages respawn timers
- `RespawnNodeCommand` - Command to respawn depleted node
- `NodeRespawnedEvent` - Event when node respawns

---

## 📋 Implementation Plan

### Phase 6.1: Trigger-Based Spawning ✅ (Partially Complete)

**Status**: Core provisioning exists, need trigger integration

**Tasks:**
- [x] Create base provisioning command/handler
- [ ] Implement `TriggerBasedSpawnService`
  - [ ] Trigger discovery by tag and local variables
  - [ ] Polygon point generation (barycentric coordinates)
  - [ ] Walkability validation
  - [ ] Weighted distribution across multiple triggers
- [ ] Update `ProvisionAreaNodesCommandHandler` to use triggers
- [ ] Add WorldConstants for trigger tags/variable names
- [ ] Update provisioning to handle CSV node_tags

**Success Criteria:**
- Nodes spawn only within designer-placed triggers
- Multiple resource types per trigger work correctly
- Larger triggers get proportionally more nodes
- All spawned locations are walkable

### Phase 6.2: Resource Discovery System 🆕

**Status**: Not started

**Tasks:**
- [ ] Create `ResourceZoneBehavior` service
  - [ ] Subscribe to trigger enter/exit events
  - [ ] Track players currently in resource zones
  - [ ] Enable/disable discovery actions based on zone presence
  - [ ] Store current zone reference per player
- [ ] Create `NodeSlotTracker` service
  - [ ] Track active node counts per trigger per resource type
  - [ ] Check if slots available (max 4 per type per trigger)
  - [ ] Update counts when nodes spawned/depleted
- [ ] Create discovery action handlers (player-initiated)
  - [ ] "Prospect for Ore" action
  - [ ] "Forage for Herbs" action
  - [ ] "Search for Timber" action
- [ ] Create `DiscoverResourceCommand` and handler
  - [ ] Support both player and NPC discoverers
- [ ] Implement knowledge-based discovery system
  - [ ] Integration with Codex knowledge system
  - [ ] Knowledge type to resource type mapping (from trigger node_tags)
  - [ ] Success rate based on knowledge relevance/level
- [ ] Discovery validation checks:
  - [ ] Discoverer (player/NPC) is in resource zone trigger
  - [ ] Discoverer has relevant knowledge for zone's node_tags
  - [ ] Node slots available (< 4 of that type in trigger)
  - [ ] Match discovery action to trigger's node_tags
- [ ] Create `NPCProspectingService`
  - [ ] Track player discovery activity per zone
  - [ ] Periodic check for low-activity zones (every 5 minutes)
  - [ ] Calculate NPC prospecting probability:
    - Time since last player discovery
    - Zone danger level
    - Available node slots
    - Zone remoteness/accessibility
  - [ ] Trigger NPC discovery attempts
  - [ ] Spawn NPC prospector actors at discovered nodes
- [ ] Create `ProspectorNPC` AI behavior
  - [ ] NPC appears when discovers node
  - [ ] Begins harvesting the discovered resource
  - [ ] Can be interacted with/attacked by players
  - [ ] Harvested resources enter NPC inventory/economy
- [ ] Create `ResourceDiscoveredEvent`
  - [ ] Support both player and NPC discoverers
- [ ] Add discovery metadata to nodes
  - [ ] Discoverer name (player or NPC name)
  - [ ] Discoverer type (player/NPC)
  - [ ] Discovery timestamp
  - [ ] Discovery method (prospecting/foraging/woodcraft)
  - [ ] Relevant knowledge used
  - [ ] Trigger source
- [ ] Player feedback messages
  - [ ] Success: "You discovered [resource]!"
  - [ ] Failure (no slots): "This area is already rich in [resource]."
  - [ ] Failure (no knowledge): "You lack the expertise to find resources here."
  - [ ] Notification when NPC discovers in nearby zone: "A prospector has arrived in [zone]"
- [ ] Knowledge XP integration

**Success Criteria:**
- Players can manually use discovery actions only when in resource zones
- Action availability updates on zone enter/exit
- Success rate scales with knowledge relevance
- Node slot limits prevent over-saturation (max 4 per type)
- Nodes only discovered in current trigger zone
- Discovery events logged and tracked
- NPCs automatically prospect in low-activity zones
- NPC prospecting creates risk/reward gameplay
- Players motivated to discover resources before NPCs do
- System provides dynamic relief from resource depletion

### Phase 6.3: Dynamic Respawn System 🆕

**Status**: Not started

**Tasks:**
- [ ] Create `NodeRespawnService` with timer management
- [ ] Implement `RespawnNodeCommand`
- [ ] Update `NodeDepletedEventHandler` to schedule respawns
- [ ] Generate new random location in same trigger
- [ ] Differentiate respawn rules:
  - [ ] Natural nodes: longer cooldown
  - [ ] Prospected nodes: shorter cooldown or one-time only?
- [ ] Create `NodeRespawnedEvent`
- [ ] Add cooldown tracking to persistence layer

**Success Criteria:**
- Depleted nodes respawn after cooldown
- Respawned nodes appear in different (but valid) locations
- Respawn events properly logged
- System handles server restarts gracefully

### Phase 6.4: Persistence Integration ✅ (Already Complete?)

**Status**: Mostly complete, verify integration

**Existing Components:**
- `PersistentResourceNodeInstance` database entity
- `ResourceNodeInstanceRepository` with full CRUD
- State tracking (uses remaining, quality, location)
- Event-driven updates

**Verification Tasks:**
- [ ] Verify trigger source metadata is persisted
- [ ] Confirm prospecting metadata saves correctly
- [ ] Test respawn state persistence
- [ ] Validate location updates on respawn

**Success Criteria:**
- All node state survives server restart
- Trigger associations maintained
- Respawn timers restored after restart
- Prospecting history preserved

### Phase 6.5: Designer Workflow & Documentation 📝

**Status**: Not started

**Tasks:**
- [ ] Document trigger setup workflow
- [ ] Create example triggers for common scenarios
- [ ] Add validation tools for designers
  - [ ] Check for overlapping triggers
  - [ ] Verify node_tags match definitions
- [ ] Create debug visualization command for DMs
  - [ ] Show all spawn triggers
  - [ ] Display node distribution
  - [ ] Test spawn generation

**Deliverables:**
- `DESIGNER_GUIDE_RESOURCE_ZONES.md`
- Example trigger templates
- DM debug commands

---

## 🔧 Technical Specifications

### Implementation Approach

**Trigger-Based Action Availability**: The system relies on NWN's persistent area triggers and Anvil's event system. A service monitors all resource zone triggers and enables/disables discovery actions based on player location.

```csharp
[ServiceBinding(typeof(ResourceZoneBehavior))]
public class ResourceZoneBehavior
{
    private readonly Dictionary<uint, NwTrigger> _playerCurrentZones = new();

    public ResourceZoneBehavior()
    {
        // Find all resource zone triggers in all areas
        foreach (var area in NwModule.Instance.Areas)
        {
            var resourceTriggers = area.Objects.OfType<NwTrigger>()
                .Where(t => t.Tag == WorldConstants.ResourceNodeZoneTag);

            foreach (var trigger in resourceTriggers)
            {
                // Subscribe to trigger events
                trigger.OnEnter += OnPlayerEnterResourceZone;
                trigger.OnExit += OnPlayerExitResourceZone;
            }
        }
    }

    private void OnPlayerEnterResourceZone(TriggerEvents.OnEnter e)
    {
        if (!e.EnteringObject.IsPlayerControlled(out var player))
            return;

        var trigger = (NwTrigger)e.TriggeringSelf;
        _playerCurrentZones[player.ObjectId] = trigger;

        // Enable discovery actions based on trigger's node_tags
        EnableDiscoveryActions(player, trigger);
    }

    private void OnPlayerExitResourceZone(TriggerEvents.OnExit e)
    {
        if (!e.ExitingObject.IsPlayerControlled(out var player))
            return;

        _playerCurrentZones.Remove(player.ObjectId);

        // Disable discovery actions
        DisableDiscoveryActions(player);
    }

    private void EnableDiscoveryActions(NwPlayer player, NwTrigger trigger)
    {
        var nodeTags = trigger.GetLocalVariable<string>(WorldConstants.LvarNodeTags).Value;
        var tags = nodeTags?.Split(',').Select(t => t.Trim()) ?? Enumerable.Empty<string>();

        // Enable appropriate actions based on what's in the zone
        if (tags.Any(t => t.StartsWith("ore") || t.StartsWith("geode")))
        {
            // Enable "Prospect for Ore" action
        }
        if (tags.Any(t => t.StartsWith("herb") || t.StartsWith("fungi")))
        {
            // Enable "Forage for Herbs" action
        }
        if (tags.Any(t => t.StartsWith("tree")))
        {
            // Enable "Search for Timber" action
        }
    }

    public NwTrigger? GetPlayerCurrentZone(NwPlayer player)
    {
        return _playerCurrentZones.TryGetValue(player.ObjectId, out var trigger)
            ? trigger
            : null;
    }
}
```

**Key Points:**
- Discovery actions only enabled when player is in a resource zone
- Actions disabled when player leaves the zone
- Player must manually use the action to attempt discovery
- Trigger's local variables define what can be discovered
- System tracks which zone each player is currently in

### WorldConstants Additions

```csharp
public static class WorldConstants
{
    // Resource Node System
    public const string ResourceNodeZoneTag = "worldengine_node_region";
    public const string LvarNodeTags = "node_tags"; // CSV of node types
    public const string LvarSpawnDensity = "spawn_density"; // Multiplier
    public const string LvarRespawnCooldown = "respawn_cooldown"; // Minutes
    public const string LvarMaxNodesPerType = "max_nodes_per_type"; // Default: 4
    public const string LvarDangerLevel = "danger_level"; // 1-10, affects NPC prospecting

    // Discovery System
    public const int DefaultMaxNodesPerType = 4; // Max nodes of one type in a trigger
    public const int NPCProspectingCheckMinutes = 5; // How often to check for NPC prospecting
    public const int LowActivityThresholdHours = 2; // Hours without player discovery = low activity
}
```

### Trigger Setup Example

```
Trigger Properties:
  Tag: "worldengine_node_region"

Local Variables:
  node_tags (string): "ore_copper,ore_iron,geode_quartz"
  spawn_density (float): 1.5  // Optional: 150% normal density
  respawn_cooldown (int): 60  // Optional: 60 min respawn
  max_nodes_per_type (int): 4  // Optional: Max nodes per type (default: 4)
  danger_level (int): 7  // Optional: 1-10, higher = NPCs less likely to prospect
```

### NWN Z-Axis Handling

**Note**: NWN's verticality system uses fixed tile heights (0, 1, 2, etc. representing elevation levels). The Z coordinate is determined by the tile's height level, not continuous 3D space. When spawning nodes:

- The trigger's Z position inherently represents the correct tile height
- Random points generated within trigger bounds automatically inherit correct Z
- No additional Z-axis validation needed - if the trigger is at the right height, spawned nodes will be too
- Walkability checks via `Location.IsWalkable` handle all terrain validation

This means our spawn system naturally respects NWN's elevation model without special handling.

### Database Schema Updates (if needed)

```sql
-- Add to PersistentResourceNodeInstance if not present:
ALTER TABLE PersistentResourceNodeInstances
ADD TriggerSource VARCHAR(64) NULL;

ALTER TABLE PersistentResourceNodeInstances
ADD ProspectedBy VARCHAR(64) NULL;

ALTER TABLE PersistentResourceNodeInstances
ADD ProspectedAt TIMESTAMP NULL;

ALTER TABLE PersistentResourceNodeInstances
ADD RespawnCooldownExpires TIMESTAMP NULL;
```

---

## 🎮 Gameplay Flow Examples

### Scenario 1: Server Startup Provisioning

1. Server starts, `AreaProvisioningService` initializes
2. After 5 seconds, scans all loaded areas
3. Finds area `zworldttemprate` in `test_settlement` region
4. Discovers 3 triggers tagged `worldengine_node_region`:
   - Forest trigger: `node_tags = "tree_oak,tree_pine"`
   - Quarry trigger: `node_tags = "ore_copper,ore_iron"`
   - Cave trigger: `node_tags = "geode_quartz,ore_gold"`
5. For each trigger:
   - Calculates weighted node count based on trigger area
   - Generates random points within polygon
   - Validates walkability
   - Creates and spawns nodes
6. Persists all nodes to database
7. Logs summary: "Provisioned 47 nodes across 3 zones in zworldttemprate"

### Scenario 2: Player Discovery (Manual Action)

1. Player Gandalf enters a mountain quarry trigger zone
2. `ResourceZoneBehavior.OnTriggerEnter` fires
3. System enables "Prospect for Ore" action for Gandalf (action now available)
4. Gandalf manually uses "Prospect for Ore" action
5. `DiscoverResourceCommand` issued with trigger reference
6. `DiscoverResourceCommandHandler` validates:
   - ✓ Gandalf is still in the trigger zone
   - ✓ Trigger has `node_tags = "ore_mithral,ore_adamant,ore_copper"`
   - ✓ Gandalf has "Expert Metallurgist" knowledge (relevant to ores)
   - Check node slots: Currently 2 mithral ore nodes, 1 adamant ore, 4 copper ore in this trigger
   - ✓ Slots available for mithral and adamant
   - ✗ Copper ore at capacity (4/4)
7. System calculates success for available ore types:
   - Success chance = 10% base + (knowledge_level × 5%) = 35%
   - Rolls: SUCCESS
   - Randomly selects between mithral and adamant
   - Result: Mithral ore
8. Generates random point within current trigger bounds
9. Creates mithral ore node at location
10. Updates `NodeSlotTracker`: Mithral ore count now 3/4 in this trigger
11. **Records player activity**: `NPCProspectingService` logs Gandalf's discovery in this zone
12. Notifies Gandalf: "Your metallurgical expertise reveals Mithral Ore Vein nearby!"
13. Updates Codex: Grants knowledge XP to "Expert Metallurgist"
14. Node marked with metadata:
    - `discovered = true`
    - `discovererType = "player"`
    - `discoverer = "Gandalf"`
    - `discoveredAt = 2025-10-28T14:30:00Z`
    - `discoveryMethod = "prospecting"`
    - `knowledgeUsed = "Expert Metallurgist"`
    - `triggerSource = "quarry_mountain_ore_zone"`

**Alternate Outcome - No Slots Available:**
If all ore types in the trigger were at capacity (4/4 each):
- Gandalf uses "Prospect for Ore"
- System checks node slots
- All ore types at 4/4 capacity
- Message: "This quarry is already rich in ore. Wait for existing veins to be depleted."
- No node spawned, no activity recorded, can try again immediately

### Scenario 2b: NPC Discovery (Low Player Activity)

1. `NPCProspectingService` runs its periodic check (every 5 minutes)
2. Examines "Deep Cave Geode Zone" trigger:
   - Last player discovery: 3 hours ago (exceeds 2-hour threshold)
   - Current node slots: 1/4 geodes present
   - Danger level: 8/10 (very dangerous)
   - Zone accessibility: Remote (requires long travel)
3. Calculate NPC prospecting chance:
   - Base chance: 20%
   - Time modifier: +30% (3 hours / 2 hour threshold × 20%)
   - Danger penalty: -40% (danger level 8)
   - Remoteness bonus: +15% (hard for players to reach)
   - Final chance: 25%
4. Rolls: SUCCESS (rolled 18, needed ≤25)
5. `DiscoverResourceCommand` issued with NPC prospector
6. System spawns alexandrite geode node at random location in trigger
7. **Spawns NPC Prospector**:
   - Name: "Grizzled Miner"
   - Location: At the discovered geode
   - Behavior: Begins harvesting the geode
   - Loot table: May drop ore/gems if defeated
8. Updates `NodeSlotTracker`: Geode count now 2/4
9. Publishes `ResourceDiscoveredEvent`:
   - `discovererType = "npc"`
   - `discoverer = "Grizzled Miner"`
   - `discoveryMethod = "prospecting"`
10. **Notifies nearby players**: "You hear the sound of mining echoing from the deep cave..."
11. Node marked with metadata:
    - `discovered = true`
    - `discovererType = "npc"`
    - `discoverer = "Grizzled Miner"`
    - `discoveredAt = 2025-10-28T17:45:00Z`
    - `discoveryMethod = "prospecting"`
    - `triggerSource = "deep_cave_geode_zone"`

**Player Response Options:**
- **Compete**: Rush to the cave before NPC harvests all resources
- **Attack**: Defeat the NPC prospector and claim the node
- **Ignore**: Let NPC harvest, resources enter economy/marketplace
- **Future**: Prospect more actively to prevent NPC competition

### Scenario 3: Node Depletion & Respawn

1. Player harvests last use from copper ore node
2. `NodeDepletedEvent` published via event bus
3. `NodeDepletedEventHandler` receives event:
   - Destroys visual placeable
   - Schedules respawn timer (60 minutes default)
4. After 60 minutes:
   - `RespawnNodeCommand` issued
   - Finds original trigger source
   - Generates NEW random location in same trigger
   - Spawns copper node at new position
5. `NodeRespawnedEvent` published
6. DMs notified: "Copper ore respawned in Hillside Quarry"

---

## 🧪 Testing Strategy

### Unit Tests

- [ ] `TriggerBasedSpawnService.GetRandomPointInTrigger()` - verify distribution
- [ ] `DiscoverResourceCommandHandler` - knowledge-based success rate calculations
- [ ] `NodeRespawnService` - timer scheduling logic
- [ ] Polygon geometry helpers - edge cases

### Integration Tests

- [ ] Full provisioning cycle with mock triggers
- [ ] Discovery system finds correct resource types based on knowledge
- [ ] Respawn places nodes in original trigger
- [ ] Persistence survives service restart

### Manual Testing Checklist

- [ ] Create test area with 3-4 different trigger zones
- [ ] Verify nodes spawn only in triggers
- [ ] Test discovery in each zone with different knowledge types
- [ ] Deplete node, verify respawn in different location
- [ ] Restart server, verify all state restored
- [ ] Check DM commands show correct information

---

## 🚧 Known Limitations & Future Work

### Current Limitations

1. **Simple Distribution** - No clustering or vein patterns (yet)
2. **Basic Validation** - Simple walkability check, could be enhanced
3. **NPC AI Simplicity** - Basic prospector behavior, could be more sophisticated

### Future Enhancements

1. **Advanced Distribution Patterns**
   - Clustered spawning (ore veins)
   - Line patterns (along rivers for herbs)
   - Scatter patterns (random trees)

2. **Environmental Factors**
   - Seasonal variations in spawn rates
   - Weather affecting discovery success
   - Time of day modifiers

3. **Economic Integration**
   - Rarer resources in high-demand trigger cooldowns
   - Market prices affect respawn rates
   - Supply/demand balancing
   - NPC-harvested resources affect market prices

4. **Advanced NPC AI**
   - NPCs form mining camps at productive sites
   - NPC guilds that compete with player guilds
   - Patrol routes between resource zones
   - Social behavior (flee, fight, negotiate)
   - NPC traders buy/sell discovered resources

5. **Discovery Mini-Game**
   - Interactive elements for discovery process
   - Tool quality affecting success
   - Environmental clues (visual/audio feedback)

6. **Discovery as Commodity**
   - "Node location maps" as tradeable items
   - Prospectors sell coordinates to harvesters
   - Guild contracts for exclusive prospecting rights
   - Information brokers who buy/sell discovery data
   - Expired maps (nodes depleted) become worthless

7. **Guild Resource Control**
   - Guilds claim territory/triggers for exclusive prospecting
   - Guild warehouses store discovered but unharvested nodes
   - Guild processing facilities turn raw ores → refined goods
   - Inter-guild trade agreements
   - Guild market manipulation (stockpiling to control prices)

---

## 📊 Success Metrics

### Performance
- [ ] Node provisioning completes within 10 seconds at startup
- [ ] < 100ms per discovery attempt
- [ ] Respawn system handles 1000+ nodes efficiently

### Gameplay
- [ ] Players can find resources reliably
- [ ] Discovery feels rewarding (not too easy/hard)
- [ ] Resources respawn at healthy rate
- [ ] No "dead zones" without resources
- [ ] Knowledge-based discovery provides meaningful interaction

### Technical
- [ ] Zero data loss on server restart
- [ ] No duplicate node spawning
- [ ] All events properly logged
- [ ] Designer workflow is intuitive

---

## 🔗 Related Systems

- **Harvesting System** (Phase 3.4) - Consumes nodes, requires skill (separate from discovery)
- **Economy System** (Phase 3.3) - Resource prices, market dynamics, guild stockpiling
- **Codex System** (Phase 3.2) - Discovery tracking, knowledge system (enables prospecting)
- **Event Bus** (Phase 3.5) - Event-driven architecture
- **Persistence** (Core) - Database layer for all state
- **Organization System** (Future) - Guild resource control, territory claims, trade agreements

---

## 📚 References

### Code Files
- `PersistentResourceNodeInstance.cs` - Database entity
- `ResourceNodeInstance.cs` - Domain aggregate
- `ResourceNodeService.cs` - Core node operations
- `ProvisionAreaNodesCommandHandler.cs` - Current provisioning (needs trigger update)
- Test region: `Resources/WorldEngine/Regions/test_settlement.json`

### Documentation
- `PHASE3_5_NODE_PROVISIONING_COMPLETE.md` - Basic provisioning (current)
- `HARVESTING_COMPLETE_FINAL.md` - Harvesting mechanics
- `EVENT_HANDLER_MARKER_IMPLEMENTATION.md` - Event bus pattern

---

## 🎬 Next Steps

1. **Implement TriggerBasedSpawnService** (highest priority)
2. **Update provisioning to use triggers** (replaces random spawning)
3. **Test with real area triggers** (proof of concept)
4. **Implement discovery system** (adds dynamic gameplay)
5. **Add respawn mechanics** (completes lifecycle)

---

## 💡 Design Philosophy

**Designer Intent First**: Trust level designers to place spawn zones intelligently. The code validates and distributes, but doesn't override artistic decisions.

**Dynamic but Predictable**: Nodes move around, but always within expected zones. Players learn where to look, but exact positions vary.

**Persistence as Truth**: Database state is authoritative. Events update it, respawns consult it, nothing circumvents it.

**Graceful Degradation**: If triggers missing, fall back to safe random spawning. Always functional, even if not optimal.

---

*This phase completes the resource node lifecycle, creating a living, breathing world where resources are discovered, harvested, and naturally regenerate.*

