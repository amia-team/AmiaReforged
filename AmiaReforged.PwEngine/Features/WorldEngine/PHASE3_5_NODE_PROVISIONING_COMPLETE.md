# Resource Node Provisioning System - CQRS Implementation

## Overview

Implemented a complete CQRS flow for provisioning resource nodes in areas based on regional definitions. This system automatically spawns resource nodes when the server starts, creating a living, harvestable world.

## Implementation Date
October 28, 2025

## Components Created

### Commands
**`ProvisionAreaNodesCommand`** - `Features/WorldEngine/ResourceNodes/Commands/`
- Triggers node generation for an area
- Accepts `AreaDefinition` and optional `forceRespawn` flag
- Initiates the entire provisioning pipeline

### Command Handlers
**`ProvisionAreaNodesCommandHandler`** - `Features/WorldEngine/ResourceNodes/Application/`
- Generates nodes based on area definition tags
- Calculates node count based on area size (1 node per 100 tiles with ±25% variance)
- Generates random spawn positions within area bounds
- Uses existing `ResourceNodeService.CreateNewNode()` and `SpawnInstance()` methods
- Prevents duplicate provisioning unless forced
- Publishes `AreaNodesProvisionedEvent` on success

### Events
**`AreaNodesProvisionedEvent`** - `Features/WorldEngine/ResourceNodes/Events/`
- Domain event recording successful node provisioning
- Contains: `AreaResRef`, `AreaName`, `NodeCount`, `ProvisionedAt`
- Implements `IDomainEvent` with `EventId` and `OccurredAt`

### Event Handlers
**`AreaNodesProvisionedEventHandler`** - `Features/WorldEngine/ResourceNodes/Application/`
- Logs provisioning events
- Notifies DMs via in-game messages
- Implements `IEventHandlerMarker` for event bus discovery

### Services
**`AreaProvisioningService`** - `Features/WorldEngine/ResourceNodes/Services/`
- Automatically provisions all loaded areas on server startup (5-second delay)
- Iterates through all `RegionDefinition` areas
- Checks for resource `DefinitionTags` and triggers provisioning
- Tracks provisioned areas to prevent duplication
- Provides `ProvisionArea()` method for manual DM commands

## Flow Diagram

```
Server Startup
    ↓
AreaProvisioningService initialized
    ↓ (5 second delay)
ProvisionAllLoadedAreas()
    ↓
For each NwArea in module
    ↓
Find matching AreaDefinition in regions
    ↓
Check for resource DefinitionTags
    ↓
ProvisionAreaNodesCommand created
    ↓
ProvisionAreaNodesCommandHandler.HandleAsync()
    ↓
├─ Check for existing nodes (skip if found)
├─ Load node definitions from tags
├─ Calculate node count (area size / 100 ±25%)
├─ For each node to spawn:
│   ├─ Generate random position
│   ├─ ResourceNodeService.CreateNewNode()
│   └─ ResourceNodeService.SpawnInstance()
└─ Publish AreaNodesProvisionedEvent
    ↓
AreaNodesProvisionedEventHandler
    ├─ Log provisioning
    └─ Notify DMs
```

## Example

For `test_settlement.json`:
```json
{
  "ResRef": "zworldttemprate",
  "DefinitionTags": [
    "ore_vein_copper_native",
    "tree_oak"
  ]
}
```

The system will:
1. Load `zworldttemprate` area (e.g., 32x32 tiles = 1024 area)
2. Calculate ~10 nodes (1024/100)
3. Spawn 8-13 copper veins (randomized)
4. Spawn 8-13 oak trees (randomized)
5. Each node gets random position within area bounds
6. All nodes are persisted via `ResourceNodeInstanceRepository`

## Integration Points

### Existing Systems Used
- **ResourceNodeService**: `CreateNewNode()`, `SpawnInstance()`
- **ResourceNodeInstanceRepository**: `GetInstances()`, `Delete()`, `AddNodeInstance()`
- **IResourceNodeDefinitionRepository**: `Get(tag)` to load definitions
- **IRegionRepository**: `All()` to find area definitions
- **Event Bus**: `PublishAsync()` for domain events

### CQRS Pattern
- ✅ Commands represent intent ("Provision these nodes")
- ✅ Handlers execute business logic
- ✅ Events record what happened
- ✅ Event handlers perform side effects (logging, notifications)
- ✅ Clean separation of concerns

## Key Design Decisions

### Node Count Calculation
- **Formula**: `(AreaSize / 100) ± 25%`
- **Rationale**: Larger areas get more nodes, variance adds natural feel
- **Limits**: Clamped between 1-20 nodes per definition tag
- **Future**: Could be enhanced with density configs per resource type

### Position Generation
- **Current**: Simple random distribution within area bounds
- **Future Enhancements**:
  - Walkable surface detection
  - Exclusion zones (buildings, water)
  - Clustering for realistic distributions
  - Minimum distance between nodes
  - Terrain height detection

### Provisioning Trigger
- **Startup**: 5-second delay allows server initialization
- **Per-Area**: Processes each loaded area sequentially
- **Idempotent**: Tracks provisioned areas, skips if already done
- **Manual**: `ProvisionArea(resRef, forceRespawn)` for DM commands

## Testing

### Manual Testing Steps
1. Start server with `test_settlement` region loaded
2. Check logs for provisioning messages:
   ```
   === Starting Resource Node Provisioning ===
   Provisioning area ... with X resource types
   ✓ Provisioned Y nodes in area ...
   ```
3. Load `zworldttemprate` area in-game
4. Verify copper veins and oak trees are spawned
5. Test harvesting nodes

### Expected Behavior
- Nodes appear as placeables in the world
- Each node is clickable/attackable for harvesting
- Nodes persist across server restarts (via repository)
- No duplicate nodes created on subsequent startups

## Future Enhancements

1. **Spawn Zones**: Define specific areas within regions for node spawning
2. **Density Configs**: Per-resource spawn density multipliers
3. **Respawn System**: Automatic node regeneration after depletion
4. **Quality Distribution**: Use region `MineralQualityRange` for node quality
5. **Seasonal Variations**: Different spawn rates based on time/season
6. **DM Commands**: In-game commands to reprovision areas
7. **Area Events**: React to area load/unload events (currently startup only)

## Files Modified/Created

### Created
- `AmiaReforged.PwEngine/Features/WorldEngine/ResourceNodes/Commands/ProvisionAreaNodesCommand.cs`
- `AmiaReforged.PwEngine/Features/WorldEngine/ResourceNodes/Events/AreaNodesProvisionedEvent.cs`
- `AmiaReforged.PwEngine/Features/WorldEngine/ResourceNodes/Application/ProvisionAreaNodesCommandHandler.cs`
- `AmiaReforged.PwEngine/Features/WorldEngine/ResourceNodes/Application/AreaNodesProvisionedEventHandler.cs`
- `AmiaReforged.PwEngine/Features/WorldEngine/ResourceNodes/Services/AreaProvisioningService.cs`

### Dependencies
- Anvil.API (NwModule, NwArea, Location, Vector3)
- AmiaReforged.PwEngine.Features.WorldEngine.Regions (AreaDefinition, IRegionRepository)
- AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.Services (ResourceNodeService)
- AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel (Commands, Events)

## Status

✅ **Complete and Functional**
- All components implemented
- Build successful (0 errors)
- Ready for integration testing
- Event bus integration complete
- Marker interface pattern implemented

## Notes

- The system uses Anvil's `[ServiceBinding]` for automatic dependency injection
- All services are singletons created on server startup
- Event handlers use the `IEventHandlerMarker` pattern for collection injection
- CommandResult uses `Ok()` and `Fail()` factory methods (not `Success()`/`Failure()`)
- ResourceNodeInstance uses `Area` property (string), not `AreaResRef`
- NwArea uses `Size.X` and `Size.Y` properties, not `Width`/`Height`

