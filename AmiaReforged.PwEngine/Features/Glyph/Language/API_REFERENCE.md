# Glyph API reference

Generated from registered Glyph contracts. Do not edit function or context tables by hand.

## Events and stages

| Source event | Runtime identity | Category | Stages |
| --- | --- | --- | --- |
| encounter.after_group_spawn | AfterGroupSpawn | Encounter |  |
| encounter.before_group_spawn | BeforeGroupSpawn | Encounter |  |
| encounter.on_boss_spawn | OnBossSpawn | Encounter |  |
| encounter.on_creature_death | OnCreatureDeath | Encounter |  |
| encounter.on_creature_spawn | OnCreatureSpawn | Encounter |  |
| interaction | InteractionPipeline | Interaction | attempted, started, tick, completed |
| trait.on_granted | OnTraitGranted | Trait |  |
| trait.on_removed | OnTraitRemoved | Trait |  |

## Functions and call aliases

### `Object.get_distance`

`Object.get_distance(object_a: Object, object_b: Object) → Float`

Returns the distance in meters between two game objects. Returns 0 if either object is invalid.

Kind: Value. Canonical: `Object.get_distance`.

Available in: encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `Object.is_player`

`Object.is_player(object: Object) → Bool`

Returns true when the object is a player character (NWScript.GetIsPC). Returns false for invalid or unresolvable objects.

Kind: Value. Canonical: `Object.is_player`.

Available in: encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `Object.nearest_object_by_type`

`Object.nearest_object_by_type(origin: Object, type: String) → Object`

Returns the nearest game object of a curated type from an origin. Supported types (case-insensitive, lowercase canonical): trigger, door, placeable, creature, waypoint. Returns OBJECT_INVALID when the origin is invalid, the type is unsupported, or no match exists.

Kind: Value. Canonical: `Object.nearest_object_by_type`.

Available in: encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `creature.ac`

`creature.ac(creature: Object) → Int`

Returns the current armor class of a creature.

Kind: Value. Canonical: `creature.ac`.

Available in: encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `creature.hp`

`creature.hp(creature: Object) → Int`

Returns the current and maximum hit points of a creature.

Kind: Value. Canonical: `creature.hp`.

Available in: encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `creature.max_hp`

`creature.max_hp(creature: Object) → Int`

Returns the current and maximum hit points of a creature.

Kind: Value. Canonical: `creature.max_hp`.

Available in: encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `creature.name`

`creature.name(creature: Object) → String`

Returns the current display name and original blueprint name of a creature.

Kind: Value. Canonical: `creature.name`.

Available in: encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `damage`

`damage(creature: Object, amount: Int = 10, damage_type: String = MAGICAL) → Void`

Deals damage of a specified type to a creature. Damage types: BLUDGEONING, PIERCING, SLASHING, FIRE, COLD, ACID, ELECTRICAL, DIVINE, NEGATIVE, POSITIVE, SONIC, MAGICAL.

Kind: Action. Canonical: `damage`.

Available in: encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `distance`

`distance(object_a: Object, object_b: Object) → Float`

Returns the distance in meters between two game objects. Returns 0 if either object is invalid.

Kind: Value. Canonical: `distance`.

Available in: encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `fail`

`fail(message: String = Interaction failed) → Void`

Fails the interaction at the current pipeline stage. During Attempted, blocks the interaction from starting. During Started/Tick/Completed, cancels the session. Terminates the execution chain.

Kind: Action. Canonical: `fail`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `floating_text`

`floating_text(creature: Object, message: String = ) → Void`

Displays floating text above a creature.

Kind: Action. Canonical: `floating_text`.

Available in: encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `has_item`

`has_item(creature: Object, item_tag: String) → Bool`

Checks if a creature has an item with the specified tag in their inventory. Returns whether any matching item exists and the total stack count.

Kind: Value. Canonical: `has_item`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `has_knowledge`

`has_knowledge(character_id: String, knowledge_tag: String) → Bool`

Returns true if the character has learned the specified knowledge article.

Kind: Value. Canonical: `has_knowledge`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `has_trait`

`has_trait(trait_tag: String = ) → Bool`

Checks if the target character has a specific trait. Returns true/false.

Kind: Value. Canonical: `has_trait`.

Available in: encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `heal`

`heal(creature: Object, amount: Int = 10) → Void`

Heals a creature for the specified amount of hit points.

Kind: Action. Canonical: `heal`.

Available in: encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `industry.is_member`

`industry.is_member(character_id: String, industry_tag: String) → Bool`

Returns true if the character is enrolled in the specified industry.

Kind: Value. Canonical: `industry.is_member`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `industry.level`

`industry.level(character_id: String, industry_tag: String) → Int`

Returns the character's proficiency level in a specific industry. Outputs the level name, numeric value, and whether they are a member.

Kind: Value. Canonical: `industry.level`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `message`

`message(creature: Object, message: String, channel: String = server) → Void`

Sends a text message to a creature. Channels: 'server' (system message), 'floating' (floating text above creature), 'shout' (speak as creature).

Kind: Action. Canonical: `message`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `metadata`

`metadata(key: String) → String`

Reads a value from the interaction session's metadata dictionary. Returns the value as a string and whether the key was found.

Kind: Value. Canonical: `metadata`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `party.members`

`party.members() → List<Object>`

Returns a list of player character object IDs in the encounter area, and their count.

Kind: Value. Canonical: `party.members`.

Available in: encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn.

### `play_vfx`

`play_vfx(target: Object, vfx_id: Int = 287, duration: Float = 0) → Void`

Plays a visual effect on a target. Use NWN VFX constant IDs. Duration 0 = instant effect, otherwise temporary for the given seconds. Common IDs: 16 (FNF_Fireball), 287 (DUR_GLOW_YELLOW), 45 (FNF_Sound_Burst).

Kind: Action. Canonical: `play_vfx`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `player.has_item`

`player.has_item(item_tag: String) → Bool`

Checks if a creature has an item with the specified tag in their inventory. Returns whether any matching item exists and the total stack count.

Kind: Value. Canonical: `has_item`. Implicit parameter: `player`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `player.has_knowledge`

`player.has_knowledge(knowledge_tag: String) → Bool`

Returns true if the character has learned the specified knowledge article.

Kind: Value. Canonical: `has_knowledge`. Implicit parameter: `context.character_id`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `random`

`random(min: Int = 1, max: Int = 100) → Int`

Generates a random integer between Min and Max (inclusive).

Kind: Value. Canonical: `random`.

Available in: encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `session_object`

`session_object(key: String) → Object`

Retrieves an NwObject (object ID) previously stored in the interaction session with the Store Session Object node. Returns OBJECT_INVALID if the key is not found.

Kind: Value. Canonical: `session_object`.

Available in: interaction/started, interaction/tick, interaction/completed.

### `set_metadata`

`set_metadata(key: String, value: String) → Void`

Writes a key-value pair to the interaction session's metadata. Metadata persists for the session's lifetime and can be read by the Get Metadata node or other interaction handlers.

Kind: Action. Canonical: `set_metadata`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `set_name`

`set_name(creature: Object, name: String) → Void`

Changes the display name of a creature.

Kind: Action. Canonical: `set_name`.

Available in: encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `set_progress`

`set_progress(new_progress: Int = 0) → Void`

Sets the interaction session's progress (tick count) to a new value. Can be used to skip ahead or reset progress.

Kind: Action. Canonical: `set_progress`.

Available in: interaction/started, interaction/tick.

### `set_required_rounds`

`set_required_rounds(new_rounds: Int = 3) → Void`

Changes the total number of rounds needed for the interaction to complete. Minimum value is 1. Can extend or shorten an interaction mid-flight.

Kind: Action. Canonical: `set_required_rounds`.

Available in: interaction/started, interaction/tick.

### `set_status`

`set_status(status: String = Completed) → Void`

Sets the interaction session's lifecycle status. Values: Active, Completed, Cancelled, Failed. Use to forcibly end or fail an interaction from a script.

Kind: Action. Canonical: `set_status`.

Available in: interaction/started, interaction/tick, interaction/completed.

### `skill_check`

`skill_check(creature: Object, skill: String = Lore, dc: Int = 15) → Bool`

Performs a skill check (rank + d20 vs DC) and branches on the result. Outputs the roll, rank, and total for downstream use. Skill names: Persuade, Intimidate, Lore, Heal, Bluff, Spot, Listen, Search, etc.

Kind: PredicateBranch. Canonical: `skill_check`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `spawn.cancel`

`spawn.cancel() → Void`

Prevents the current spawn group from spawning. Only works in BeforeGroupSpawn graphs.

Kind: Action. Canonical: `spawn.cancel`.

Available in: encounter.before_group_spawn.

### `spawn.modify_count`

`spawn.modify_count(new_count: Int = 1) → Void`

Changes the number of creatures that will spawn for this group. Only works in BeforeGroupSpawn graphs.

Kind: Action. Canonical: `spawn.modify_count`.

Available in: encounter.before_group_spawn.

### `spawn.skip_bonuses`

`spawn.skip_bonuses() → Void`

Prevents the data-driven bonus pipeline from being applied to this creature. Only works in OnCreatureSpawn and OnBossSpawn graphs. Use this when the Glyph graph applies its own custom bonuses.

Kind: Action. Canonical: `spawn.skip_bonuses`.

Available in: encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `spawn.skip_mutations`

`spawn.skip_mutations() → Void`

Prevents the data-driven mutation pipeline from being applied to this creature. Only works in OnCreatureSpawn graphs. Use this when the Glyph graph applies its own custom mutations or you want the creature unmodified.

Kind: Action. Canonical: `spawn.skip_mutations`.

Available in: encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `spawn_resource_node`

`spawn_resource_node(trigger: Object) → Void`

Spawns a single resource node inside a worldengine_node_region trigger, pulling from the area's resource definitions filtered by the trigger's node_tags. If the object is not a valid trigger or no matching definitions exist, success is false.

Kind: Action. Canonical: `spawn_resource_node`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### `store_session_object`

`store_session_object(key: String, object: Object) → Void`

Stores an NwObject (object ID) in the interaction session under a string key. The stored object persists across pipeline stages and can be retrieved with the Retrieve Session Object node.

Kind: Action. Canonical: `store_session_object`.

Available in: interaction/started, interaction/tick, interaction/completed.

## Receiver methods

| Receiver | Method | Parameters | Returns | Canonical | Availability |
| --- | --- | --- | --- | --- | --- |
| Object | get_distance | object_b: Object | Float | Object.get_distance | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Object | get_nearest_object_by_type | type: String | Object | Object.nearest_object_by_type | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Object | is_player |  | Bool | Object.is_player | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |

## Context and property aliases

### encounter.after_group_spawn

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| area_resref | String | area_resref |  | Area ResRef |
| chaos.area_resref | String | area_resref |  | Area ResRef |
| chaos.corruption | Int | corruption |  | Chaos: Corruption |
| chaos.danger | Int | danger |  | Chaos: Danger |
| chaos.density | Int | density |  | Chaos: Density |
| chaos.game_time | Float | game_time |  | Game Time (hours) |
| chaos.group_name | String | group_name |  | Group Name |
| chaos.mutation | Int | mutation |  | Chaos: Mutation |
| chaos.party_size | Int | party_size |  | Party Size |
| chaos.profile_name | String | profile_name |  | Profile Name |
| chaos.spawn_count | Int | spawn_count |  | Spawn Count |
| chaos.spawned_creatures | List<Object> | spawned_creatures |  | Spawned Creatures |
| chaos.triggering_player | Object | triggering_player |  | Triggering Player |
| context.area_resref | String | area_resref |  | Area ResRef |
| context.corruption | Int | corruption |  | Chaos: Corruption |
| context.danger | Int | danger |  | Chaos: Danger |
| context.density | Int | density |  | Chaos: Density |
| context.game_time | Float | game_time |  | Game Time (hours) |
| context.group_name | String | group_name |  | Group Name |
| context.mutation | Int | mutation |  | Chaos: Mutation |
| context.party_size | Int | party_size |  | Party Size |
| context.profile_name | String | profile_name |  | Profile Name |
| context.spawn_count | Int | spawn_count |  | Spawn Count |
| context.spawned_creatures | List<Object> | spawned_creatures |  | Spawned Creatures |
| context.triggering_player | Object | triggering_player |  | Triggering Player |
| corruption | Int | corruption |  | Chaos: Corruption |
| danger | Int | danger |  | Chaos: Danger |
| density | Int | density |  | Chaos: Density |
| game_time | Float | game_time |  | Game Time (hours) |
| group_name | String | group_name |  | Group Name |
| mutation | Int | mutation |  | Chaos: Mutation |
| party.members | List<Object> | party.members |  | Returns a list of player character object IDs in the encounter area, and their count. |
| party.size | Int | party_size |  | Party Size |
| party_size | Int | party_size |  | Party Size |
| player | Object | triggering_player |  | Triggering Player |
| profile_name | String | profile_name |  | Profile Name |
| spawn.count | Int | spawn_count |  | Spawn Count |
| spawn_count | Int | spawn_count |  | Spawn Count |
| spawned_creatures | List<Object> | spawned_creatures |  | Spawned Creatures |
| time.hour | Float | game_time |  | Game Time (hours) |
| triggering_player | Object | triggering_player |  | Triggering Player |

### encounter.before_group_spawn

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| area_resref | String | area_resref |  | Area ResRef |
| chaos.area_resref | String | area_resref |  | Area ResRef |
| chaos.corruption | Int | corruption |  | Chaos: Corruption |
| chaos.danger | Int | danger |  | Chaos: Danger |
| chaos.density | Int | density |  | Chaos: Density |
| chaos.game_time | Float | game_time |  | Game Time (hours) |
| chaos.group_name | String | group_name |  | Group Name |
| chaos.is_in_region | Bool | is_in_region |  | Is In Region |
| chaos.mutation | Int | mutation |  | Chaos: Mutation |
| chaos.party_size | Int | party_size |  | Party Size |
| chaos.profile_name | String | profile_name |  | Profile Name |
| chaos.region_tag | String | region_tag |  | Region Tag |
| chaos.spawn_count | Int | spawn_count |  | Spawn Count |
| chaos.triggering_player | Object | triggering_player |  | Triggering Player |
| context.area_resref | String | area_resref |  | Area ResRef |
| context.corruption | Int | corruption |  | Chaos: Corruption |
| context.danger | Int | danger |  | Chaos: Danger |
| context.density | Int | density |  | Chaos: Density |
| context.game_time | Float | game_time |  | Game Time (hours) |
| context.group_name | String | group_name |  | Group Name |
| context.is_in_region | Bool | is_in_region |  | Is In Region |
| context.mutation | Int | mutation |  | Chaos: Mutation |
| context.party_size | Int | party_size |  | Party Size |
| context.profile_name | String | profile_name |  | Profile Name |
| context.region_tag | String | region_tag |  | Region Tag |
| context.spawn_count | Int | spawn_count |  | Spawn Count |
| context.triggering_player | Object | triggering_player |  | Triggering Player |
| corruption | Int | corruption |  | Chaos: Corruption |
| danger | Int | danger |  | Chaos: Danger |
| density | Int | density |  | Chaos: Density |
| game_time | Float | game_time |  | Game Time (hours) |
| group_name | String | group_name |  | Group Name |
| is_in_region | Bool | is_in_region |  | Is In Region |
| mutation | Int | mutation |  | Chaos: Mutation |
| party.members | List<Object> | party.members |  | Returns a list of player character object IDs in the encounter area, and their count. |
| party.size | Int | party_size |  | Party Size |
| party_size | Int | party_size |  | Party Size |
| player | Object | triggering_player |  | Triggering Player |
| profile_name | String | profile_name |  | Profile Name |
| region_tag | String | region_tag |  | Region Tag |
| spawn.count | Int | spawn_count |  | Spawn Count |
| spawn_count | Int | spawn_count |  | Spawn Count |
| time.hour | Float | game_time |  | Game Time (hours) |
| triggering_player | Object | triggering_player |  | Triggering Player |

### encounter.on_boss_spawn

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| area_resref | String | area_resref |  | Area ResRef |
| chaos.area_resref | String | area_resref |  | Area ResRef |
| chaos.corruption | Int | corruption |  | Chaos: Corruption |
| chaos.creature | Object | creature |  | Boss Creature |
| chaos.creature_resref | String | creature_resref |  | Boss ResRef |
| chaos.danger | Int | danger |  | Chaos: Danger |
| chaos.density | Int | density |  | Chaos: Density |
| chaos.mutation | Int | mutation |  | Chaos: Mutation |
| chaos.party_size | Int | party_size |  | Party Size |
| chaos.profile_name | String | profile_name |  | Profile Name |
| chaos.triggering_player | Object | triggering_player |  | Triggering Player |
| context.area_resref | String | area_resref |  | Area ResRef |
| context.corruption | Int | corruption |  | Chaos: Corruption |
| context.creature | Object | creature |  | Boss Creature |
| context.creature_resref | String | creature_resref |  | Boss ResRef |
| context.danger | Int | danger |  | Chaos: Danger |
| context.density | Int | density |  | Chaos: Density |
| context.mutation | Int | mutation |  | Chaos: Mutation |
| context.party_size | Int | party_size |  | Party Size |
| context.profile_name | String | profile_name |  | Profile Name |
| context.triggering_player | Object | triggering_player |  | Triggering Player |
| corruption | Int | corruption |  | Chaos: Corruption |
| creature | Object | creature |  | Boss Creature |
| creature.ac | Int | creature.ac |  | Returns the current armor class of a creature. |
| creature.hp | Int | creature.hp |  | Returns the current and maximum hit points of a creature. |
| creature.max_hp | Int | creature.max_hp |  | Returns the current and maximum hit points of a creature. |
| creature.name | String | creature.name |  | Returns the current display name and original blueprint name of a creature. |
| creature_resref | String | creature_resref |  | Boss ResRef |
| danger | Int | danger |  | Chaos: Danger |
| density | Int | density |  | Chaos: Density |
| mutation | Int | mutation |  | Chaos: Mutation |
| party.members | List<Object> | party.members |  | Returns a list of player character object IDs in the encounter area, and their count. |
| party.size | Int | party_size |  | Party Size |
| party_size | Int | party_size |  | Party Size |
| player | Object | triggering_player |  | Triggering Player |
| profile_name | String | profile_name |  | Profile Name |
| triggering_player | Object | triggering_player |  | Triggering Player |

### encounter.on_creature_death

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| area_resref | String | area_resref |  | Area ResRef |
| chaos.area_resref | String | area_resref |  | Area ResRef |
| chaos.corruption | Int | corruption |  | Chaos: Corruption |
| chaos.danger | Int | danger |  | Chaos: Danger |
| chaos.dead_creature | Object | dead_creature |  | Dead Creature |
| chaos.density | Int | density |  | Chaos: Density |
| chaos.group_name | String | group_name |  | Group Name |
| chaos.killer | Object | killer |  | Killer |
| chaos.mutation | Int | mutation |  | Chaos: Mutation |
| chaos.party_size | Int | party_size |  | Party Size |
| chaos.profile_name | String | profile_name |  | Profile Name |
| chaos.triggering_player | Object | triggering_player |  | Triggering Player |
| context.area_resref | String | area_resref |  | Area ResRef |
| context.corruption | Int | corruption |  | Chaos: Corruption |
| context.danger | Int | danger |  | Chaos: Danger |
| context.dead_creature | Object | dead_creature |  | Dead Creature |
| context.density | Int | density |  | Chaos: Density |
| context.group_name | String | group_name |  | Group Name |
| context.killer | Object | killer |  | Killer |
| context.mutation | Int | mutation |  | Chaos: Mutation |
| context.party_size | Int | party_size |  | Party Size |
| context.profile_name | String | profile_name |  | Profile Name |
| context.triggering_player | Object | triggering_player |  | Triggering Player |
| corruption | Int | corruption |  | Chaos: Corruption |
| danger | Int | danger |  | Chaos: Danger |
| dead_creature | Object | dead_creature |  | Dead Creature |
| density | Int | density |  | Chaos: Density |
| group_name | String | group_name |  | Group Name |
| killer | Object | killer |  | Killer |
| mutation | Int | mutation |  | Chaos: Mutation |
| party.members | List<Object> | party.members |  | Returns a list of player character object IDs in the encounter area, and their count. |
| party.size | Int | party_size |  | Party Size |
| party_size | Int | party_size |  | Party Size |
| player | Object | triggering_player |  | Triggering Player |
| profile_name | String | profile_name |  | Profile Name |
| triggering_player | Object | triggering_player |  | Triggering Player |

### encounter.on_creature_spawn

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| area_resref | String | area_resref |  | Area ResRef |
| chaos.area_resref | String | area_resref |  | Area ResRef |
| chaos.corruption | Int | corruption |  | Chaos: Corruption |
| chaos.creature | Object | creature |  | Creature |
| chaos.creature_resref | String | creature_resref |  | Creature ResRef |
| chaos.danger | Int | danger |  | Chaos: Danger |
| chaos.density | Int | density |  | Chaos: Density |
| chaos.game_time | Float | game_time |  | Game Time (hours) |
| chaos.group_name | String | group_name |  | Group Name |
| chaos.is_boss | Bool | is_boss |  | Is Boss |
| chaos.mutation | Int | mutation |  | Chaos: Mutation |
| chaos.party_size | Int | party_size |  | Party Size |
| chaos.profile_name | String | profile_name |  | Profile Name |
| chaos.spawn_index | Int | spawn_index |  | Spawn Index |
| chaos.total_count | Int | total_count |  | Total Count |
| chaos.triggering_player | Object | triggering_player |  | Triggering Player |
| context.area_resref | String | area_resref |  | Area ResRef |
| context.corruption | Int | corruption |  | Chaos: Corruption |
| context.creature | Object | creature |  | Creature |
| context.creature_resref | String | creature_resref |  | Creature ResRef |
| context.danger | Int | danger |  | Chaos: Danger |
| context.density | Int | density |  | Chaos: Density |
| context.game_time | Float | game_time |  | Game Time (hours) |
| context.group_name | String | group_name |  | Group Name |
| context.is_boss | Bool | is_boss |  | Is Boss |
| context.mutation | Int | mutation |  | Chaos: Mutation |
| context.party_size | Int | party_size |  | Party Size |
| context.profile_name | String | profile_name |  | Profile Name |
| context.spawn_index | Int | spawn_index |  | Spawn Index |
| context.total_count | Int | total_count |  | Total Count |
| context.triggering_player | Object | triggering_player |  | Triggering Player |
| corruption | Int | corruption |  | Chaos: Corruption |
| creature | Object | creature |  | Creature |
| creature.ac | Int | creature.ac |  | Returns the current armor class of a creature. |
| creature.hp | Int | creature.hp |  | Returns the current and maximum hit points of a creature. |
| creature.max_hp | Int | creature.max_hp |  | Returns the current and maximum hit points of a creature. |
| creature.name | String | creature.name |  | Returns the current display name and original blueprint name of a creature. |
| creature_resref | String | creature_resref |  | Creature ResRef |
| danger | Int | danger |  | Chaos: Danger |
| density | Int | density |  | Chaos: Density |
| game_time | Float | game_time |  | Game Time (hours) |
| group_name | String | group_name |  | Group Name |
| is_boss | Bool | is_boss |  | Is Boss |
| mutation | Int | mutation |  | Chaos: Mutation |
| party.members | List<Object> | party.members |  | Returns a list of player character object IDs in the encounter area, and their count. |
| party.size | Int | party_size |  | Party Size |
| party_size | Int | party_size |  | Party Size |
| player | Object | triggering_player |  | Triggering Player |
| profile_name | String | profile_name |  | Profile Name |
| spawn_index | Int | spawn_index |  | Spawn Index |
| time.hour | Float | game_time |  | Game Time (hours) |
| total_count | Int | total_count |  | Total Count |
| triggering_player | Object | triggering_player |  | Triggering Player |

### interaction/attempted

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| area_resref | String | area_resref |  | Area ResRef |
| chaos.area_resref | String | area_resref |  | Area ResRef |
| chaos.character_id | String | character_id |  | Character ID |
| chaos.creature | Object | creature |  | Creature |
| chaos.interaction_tag | String | interaction_tag |  | Interaction Tag |
| chaos.proficiency | String | proficiency |  | Proficiency |
| chaos.target_id | String | target_id |  | Target ID |
| chaos.target_mode | String | target_mode |  | Target Mode |
| character_id | String | character_id |  | Character ID |
| context.area_resref | String | area_resref |  | Area ResRef |
| context.character_id | String | character_id |  | Character ID |
| context.creature | Object | creature |  | Creature |
| context.interaction_tag | String | interaction_tag |  | Interaction Tag |
| context.proficiency | String | proficiency |  | Proficiency |
| context.target_id | String | target_id |  | Target ID |
| context.target_mode | String | target_mode |  | Target Mode |
| creature | Object | creature |  | Creature |
| creature.ac | Int | creature.ac |  | Returns the current armor class of a creature. |
| creature.hp | Int | creature.hp |  | Returns the current and maximum hit points of a creature. |
| creature.max_hp | Int | creature.max_hp |  | Returns the current and maximum hit points of a creature. |
| creature.name | String | creature.name |  | Returns the current display name and original blueprint name of a creature. |
| interaction_tag | String | interaction_tag |  | Interaction Tag |
| player | Object | creature |  | Creature |
| proficiency | String | proficiency |  | Proficiency |
| target_id | String | target_id |  | Target ID |
| target_mode | String | target_mode |  | Target Mode |

### interaction/completed

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| area_resref | String | area_resref |  | Area ResRef |
| chaos.area_resref | String | area_resref |  | Area ResRef |
| chaos.character_id | String | character_id |  | Character ID |
| chaos.creature | Object | creature |  | Creature |
| chaos.interaction_tag | String | interaction_tag |  | Interaction Tag |
| chaos.proficiency | String | proficiency |  | Proficiency |
| chaos.response_tag | String | response_tag |  | Response Tag |
| chaos.session_id | String | session_id |  | Session ID |
| chaos.target_id | String | target_id |  | Target ID |
| character_id | String | character_id |  | Character ID |
| context.area_resref | String | area_resref |  | Area ResRef |
| context.character_id | String | character_id |  | Character ID |
| context.creature | Object | creature |  | Creature |
| context.interaction_tag | String | interaction_tag |  | Interaction Tag |
| context.proficiency | String | proficiency |  | Proficiency |
| context.response_tag | String | response_tag |  | Response Tag |
| context.session_id | String | session_id |  | Session ID |
| context.target_id | String | target_id |  | Target ID |
| creature | Object | creature |  | Creature |
| creature.ac | Int | creature.ac |  | Returns the current armor class of a creature. |
| creature.hp | Int | creature.hp |  | Returns the current and maximum hit points of a creature. |
| creature.max_hp | Int | creature.max_hp |  | Returns the current and maximum hit points of a creature. |
| creature.name | String | creature.name |  | Returns the current display name and original blueprint name of a creature. |
| interaction_tag | String | interaction_tag |  | Interaction Tag |
| player | Object | creature |  | Creature |
| proficiency | String | proficiency |  | Proficiency |
| response_tag | String | response_tag |  | Response Tag |
| session_id | String | session_id |  | Session ID |
| target_id | String | target_id |  | Target ID |

### interaction/started

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| area_resref | String | area_resref |  | Area ResRef |
| chaos.area_resref | String | area_resref |  | Area ResRef |
| chaos.character_id | String | character_id |  | Character ID |
| chaos.creature | Object | creature |  | Creature |
| chaos.interaction_tag | String | interaction_tag |  | Interaction Tag |
| chaos.proficiency | String | proficiency |  | Proficiency |
| chaos.required_rounds | Int | required_rounds |  | Required Rounds |
| chaos.session_id | String | session_id |  | Session ID |
| chaos.target_id | String | target_id |  | Target ID |
| chaos.target_mode | String | target_mode |  | Target Mode |
| character_id | String | character_id |  | Character ID |
| context.area_resref | String | area_resref |  | Area ResRef |
| context.character_id | String | character_id |  | Character ID |
| context.creature | Object | creature |  | Creature |
| context.interaction_tag | String | interaction_tag |  | Interaction Tag |
| context.proficiency | String | proficiency |  | Proficiency |
| context.required_rounds | Int | required_rounds |  | Required Rounds |
| context.session_id | String | session_id |  | Session ID |
| context.target_id | String | target_id |  | Target ID |
| context.target_mode | String | target_mode |  | Target Mode |
| creature | Object | creature |  | Creature |
| creature.ac | Int | creature.ac |  | Returns the current armor class of a creature. |
| creature.hp | Int | creature.hp |  | Returns the current and maximum hit points of a creature. |
| creature.max_hp | Int | creature.max_hp |  | Returns the current and maximum hit points of a creature. |
| creature.name | String | creature.name |  | Returns the current display name and original blueprint name of a creature. |
| interaction_tag | String | interaction_tag |  | Interaction Tag |
| player | Object | creature |  | Creature |
| proficiency | String | proficiency |  | Proficiency |
| required_rounds | Int | required_rounds | set_required_rounds | Required Rounds |
| session_id | String | session_id |  | Session ID |
| target_id | String | target_id |  | Target ID |
| target_mode | String | target_mode |  | Target Mode |

### interaction/tick

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| area_resref | String | area_resref |  | Area ResRef |
| chaos.area_resref | String | area_resref |  | Area ResRef |
| chaos.character_id | String | character_id |  | Character ID |
| chaos.creature | Object | creature |  | Creature |
| chaos.interaction_tag | String | interaction_tag |  | Interaction Tag |
| chaos.proficiency | String | proficiency |  | Proficiency |
| chaos.progress | Int | progress |  | Progress |
| chaos.required_rounds | Int | required_rounds |  | Required Rounds |
| chaos.session_id | String | session_id |  | Session ID |
| chaos.target_id | String | target_id |  | Target ID |
| character_id | String | character_id |  | Character ID |
| context.area_resref | String | area_resref |  | Area ResRef |
| context.character_id | String | character_id |  | Character ID |
| context.creature | Object | creature |  | Creature |
| context.interaction_tag | String | interaction_tag |  | Interaction Tag |
| context.proficiency | String | proficiency |  | Proficiency |
| context.progress | Int | progress |  | Progress |
| context.required_rounds | Int | required_rounds |  | Required Rounds |
| context.session_id | String | session_id |  | Session ID |
| context.target_id | String | target_id |  | Target ID |
| creature | Object | creature |  | Creature |
| creature.ac | Int | creature.ac |  | Returns the current armor class of a creature. |
| creature.hp | Int | creature.hp |  | Returns the current and maximum hit points of a creature. |
| creature.max_hp | Int | creature.max_hp |  | Returns the current and maximum hit points of a creature. |
| creature.name | String | creature.name |  | Returns the current display name and original blueprint name of a creature. |
| interaction_tag | String | interaction_tag |  | Interaction Tag |
| player | Object | creature |  | Creature |
| proficiency | String | proficiency |  | Proficiency |
| progress | Int | progress | set_progress | Progress |
| required_rounds | Int | required_rounds | set_required_rounds | Required Rounds |
| session_id | String | session_id |  | Session ID |
| target_id | String | target_id |  | Target ID |

### trait.on_granted

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| chaos.character_id | String | character_id |  | Character ID |
| chaos.target_creature | Object | target_creature |  | Target Creature |
| chaos.trait_tag | String | trait_tag |  | Trait Tag |
| character_id | String | character_id |  | Character ID |
| context.character_id | String | character_id |  | Character ID |
| context.target_creature | Object | target_creature |  | Target Creature |
| context.trait_tag | String | trait_tag |  | Trait Tag |
| creature | Object | target_creature |  | Target Creature |
| creature.ac | Int | creature.ac |  | Returns the current armor class of a creature. |
| creature.hp | Int | creature.hp |  | Returns the current and maximum hit points of a creature. |
| creature.max_hp | Int | creature.max_hp |  | Returns the current and maximum hit points of a creature. |
| creature.name | String | creature.name |  | Returns the current display name and original blueprint name of a creature. |
| target_creature | Object | target_creature |  | Target Creature |
| trait_tag | String | trait_tag |  | Trait Tag |

### trait.on_removed

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| chaos.character_id | String | character_id |  | Character ID |
| chaos.target_creature | Object | target_creature |  | Target Creature |
| chaos.trait_tag | String | trait_tag |  | Trait Tag |
| character_id | String | character_id |  | Character ID |
| context.character_id | String | character_id |  | Character ID |
| context.target_creature | Object | target_creature |  | Target Creature |
| context.trait_tag | String | trait_tag |  | Trait Tag |
| creature | Object | target_creature |  | Target Creature |
| creature.ac | Int | creature.ac |  | Returns the current armor class of a creature. |
| creature.hp | Int | creature.hp |  | Returns the current and maximum hit points of a creature. |
| creature.max_hp | Int | creature.max_hp |  | Returns the current and maximum hit points of a creature. |
| creature.name | String | creature.name |  | Returns the current display name and original blueprint name of a creature. |
| target_creature | Object | target_creature |  | Target Creature |
| trait_tag | String | trait_tag |  | Trait Tag |

## Writable state

| Name | Type | Setter | Availability |
| --- | --- | --- | --- |
| progress | Int | set_progress | interaction/started, interaction/tick |
| required_rounds | Int | set_required_rounds | interaction/started, interaction/tick |
| status | String | set_status | interaction/started, interaction/tick, interaction/completed |

## Indexers

| Name | Getter | Setter |
| --- | --- | --- |
| metadata[index] | metadata | set_metadata |
