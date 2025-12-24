# Process-Based Crafting System

A Fantasy Life-inspired crafting system where player timing and action choices during a progress bar affect the final item's quality and traits.

---

## 📚 Quick Links

| Concept | Description |
|---------|-------------|
| [Design Philosophy](#design-philosophy) | Why this pattern was chosen |
| [Progress Bar Model](#-progress-bar-crafting-model) | Core gameplay loop |
| [Process Graphs](#-process-graphs) | Linear, branching, and cyclic structures |
| [Core Concepts](#core-concepts) | CraftingProgress, ActionWindow, ProcessGraph |
| [JSON Definitions](#-json-process-definitions) | Data-driven process definitions |
| [Knowledge Integration](#-knowledge-integration) | How Knowledge enhances crafting |
| [Implementation Roadmap](#-implementation-roadmap) | Phased development plan |

---

## 🎯 Design Philosophy

### The Problem with Traditional Crafting

Traditional crafting systems use a simple **items in → items out** model:

```
[Iron Ore] + [Coal] → [Iron Ingot]
```

This is functional but lacks depth. Players follow recipes mechanically without meaningful choices during the crafting process itself.

### Our Solution: Timed Action Crafting

Inspired by **Fantasy Life**, crafting becomes an interactive minigame:

1. **Progress Bar** — A value advances from `0.0` to `100.0`
2. **Action Windows** — At certain progress ranges, actions become available
3. **Timing Matters** — Optimal timing gives bonuses; mistiming causes penalties
4. **Process Graphs** — Recipes define branching/cyclic paths through action sequences

This gives us:
- **Skill expression** — Player timing affects quality
- **Emergent outcomes** — Different paths through the graph produce different traits
- **Discovery through experimentation** — Players learn optimal sequences
- **Knowledge enhancement** — Learned Knowledge widens optimal windows and reduces penalties

---

## 🎮 Progress Bar Crafting Model

Crafting uses a **progress bar** that tracks where you are in the crafting process. The bar advances automatically, but **there is no time pressure** — the progress bar is a position tracker, not a timer.

### Key Design Principle

> **Engaged players get better outcomes; passive players still succeed.**

- **Interactive Mode** — Player applies actions at the right progress points → bonus quality/traits
- **Auto-Craft Mode** — Progress bar completes without interaction → baseline quality for skill tier

This means crafting is **never punishing** — you can always walk away and get a usable item. But players who engage with the minigame are rewarded with superior results.

### How It Works

1. **Start Crafting** — Player selects a recipe and crafting station, consuming ingredients
2. **Progress Bar Begins** — A progress value advances from `0.0` to `100.0`
3. **Action Windows** — At certain progress ranges, specific actions become available
4. **Player Choice** — Apply actions for bonuses, or let the bar advance passively
5. **Completion** — When progress reaches 100.0, the item is finalized

### Interactive vs. Auto-Craft

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        INTERACTIVE CRAFTING                             │
├─────────────────────────────────────────────────────────────────────────┤
│  Player applies HAMMER at progress 35.0 (optimal window: 30-40)         │
│  → +0.15 quality bonus                                                  │
│  → "Well-Shaped" trait added                                            │
│  → Message: "Perfect strikes shape the blade beautifully!"              │
├─────────────────────────────────────────────────────────────────────────┤
│                         AUTO-CRAFT (PASSIVE)                            │
├─────────────────────────────────────────────────────────────────────────┤
│  Player does nothing, progress bar reaches 100.0                        │
│  → Baseline quality (1.0 × skill tier modifier)                         │
│  → No bonus traits                                                      │
│  → Item is still successfully crafted                                   │
└─────────────────────────────────────────────────────────────────────────┘
```

### Misapplied Actions

Applying an action at the **wrong progress point** results in penalties:

```
Progress Bar (0.0 ──────────────────────────────────────────► 100.0)

  [0-15]     [20-35]      [40-55]       [60-75]      [80-95]
    │           │            │             │            │
  HEAT      HAMMER       HAMMER         QUENCH       GRIND
            (optimal)    (good)       (optimal)    (optional)
                │            │             │
              +0.15        +0.05        +0.10
             quality      quality      quality

  ⚠️ HAMMER at [60-75] = -0.10 quality (metal too cool!)
  ⚠️ QUENCH at [20-35] = -0.20 quality (too early, brittle!)
  ✓  No action at all  = 0.00 quality (baseline, no penalty)
```

**Important:** Doing nothing is always safe. Penalties only apply when you actively mistime an action.

### Quality Calculation

```
Final Quality = Base Quality × Skill Tier Modifier × (1.0 + Σ Action Bonuses)

Where:
- Base Quality = 1.0
- Skill Tier Modifier = Based on ProficiencyLevel (Novice: 0.8, Expert: 1.2, Master: 1.5, etc.)
- Action Bonuses = Sum of all optimal/standard/mistime modifiers from applied actions
```

**Examples:**

| Scenario | Skill Tier | Actions | Final Quality |
|----------|------------|---------|---------------|
| Auto-craft | Apprentice (1.0) | None | 1.0 |
| Auto-craft | Expert (1.2) | None | 1.2 |
| Engaged | Apprentice (1.0) | +0.15, +0.10, +0.08 | 1.33 |
| Engaged | Expert (1.2) | +0.15, +0.10, +0.08 | 1.60 |
| Mistimed | Expert (1.2) | +0.15, -0.20 | 1.14 |

### Skill Tier Baseline Modifiers

| ProficiencyLevel | Baseline Modifier | Notes |
|------------------|-------------------|-------|
| Layman | 0.6 | Poor quality, frequent flaws |
| Novice | 0.8 | Basic quality |
| Apprentice | 1.0 | Standard quality |
| Journeyman | 1.1 | Good quality |
| Expert | 1.2 | High quality |
| Master | 1.5 | Exceptional quality |
| Grandmaster | 1.8 | Legendary quality |

### Core Classes

#### CraftingProgress

Tracks the current state of an active crafting attempt.

```csharp
public class CraftingProgress
{
    /// <summary>
    /// Current progress value (0.0 to 100.0)
    /// </summary>
    public float CurrentProgress { get; private set; } = 0f;

    /// <summary>
    /// Rate of progress per tick (can be modified by actions)
    /// </summary>
    public float ProgressRate { get; set; } = 1.0f;

    /// <summary>
    /// Whether the progress bar is currently advancing
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Actions applied during this crafting attempt with their timing
    /// </summary>
    public List<TimedAction> AppliedActions { get; } = [];

    public void Advance(float deltaTime)
    {
        if (!IsActive) return;
        CurrentProgress = Math.Min(100f, CurrentProgress + (ProgressRate * deltaTime));
    }

    public bool IsComplete => CurrentProgress >= 100f;
}

public record TimedAction(string ActionTag, float AppliedAtProgress, ActionResult Result);
```

#### CraftingContext

The evolving state during a crafting session.

```csharp
public class CraftingContext
{
    public required Recipe TargetRecipe { get; init; }

    /// <summary>
    /// The crafter's proficiency level, determines baseline quality
    /// </summary>
    public required ProficiencyLevel CrafterProficiency { get; init; }

    /// <summary>
    /// Flexible property bag for process-specific state
    /// </summary>
    public Dictionary<string, object> Properties { get; } = new();

    /// <summary>
    /// Accumulated quality modifier from actions (can be positive or negative)
    /// </summary>
    public float QualityModifier { get; set; } = 0f;

    /// <summary>
    /// Traits earned during crafting
    /// </summary>
    public List<string> Traits { get; } = [];

    /// <summary>
    /// Knowledge tags applied to this session (for bonuses)
    /// </summary>
    public List<string> AppliedKnowledgeTags { get; } = [];

    /// <summary>
    /// Calculates final quality based on skill tier and accumulated bonuses
    /// </summary>
    public float CalculateFinalQuality()
    {
        float skillTierModifier = GetSkillTierModifier(CrafterProficiency);
        return skillTierModifier * (1.0f + QualityModifier);
    }

    private static float GetSkillTierModifier(ProficiencyLevel level) => level switch
    {
        ProficiencyLevel.Layman => 0.6f,
        ProficiencyLevel.Novice => 0.8f,
        ProficiencyLevel.Apprentice => 1.0f,
        ProficiencyLevel.Journeyman => 1.1f,
        ProficiencyLevel.Expert => 1.2f,
        ProficiencyLevel.Master => 1.5f,
        ProficiencyLevel.Grandmaster => 1.8f,
        _ => 1.0f
    };
}
```

#### ActionWindow

Defines when an action can be applied and its effects based on timing.

```csharp
public class ActionWindow
{
    /// <summary>
    /// The action tag (e.g., "hammer", "quench", "polish")
    /// </summary>
    public required string ActionTag { get; init; }

    /// <summary>
    /// Start of the window (0.0 to 100.0)
    /// </summary>
    public required float WindowStart { get; init; }

    /// <summary>
    /// End of the window (0.0 to 100.0)
    /// </summary>
    public required float WindowEnd { get; init; }

    /// <summary>
    /// Optimal range within the window for best results
    /// </summary>
    public float? OptimalStart { get; init; }
    public float? OptimalEnd { get; init; }

    /// <summary>
    /// Quality modifier when applied in optimal range
    /// </summary>
    public float OptimalQualityBonus { get; init; } = 0.15f;

    /// <summary>
    /// Quality modifier when applied in window but outside optimal
    /// </summary>
    public float StandardQualityBonus { get; init; } = 0.05f;

    /// <summary>
    /// Traits added when action is applied optimally
    /// </summary>
    public List<string> OptimalTraits { get; init; } = [];

    /// <summary>
    /// Message shown on optimal timing
    /// </summary>
    public string? OptimalMessage { get; init; }

    /// <summary>
    /// Message shown on standard timing
    /// </summary>
    public string? StandardMessage { get; init; }
}
```

#### MistimeEffect

Defines penalties for applying an action at the wrong time.

```csharp
public class MistimeEffect
{
    /// <summary>
    /// The action tag this applies to
    /// </summary>
    public required string ActionTag { get; init; }

    /// <summary>
    /// Progress range where this action is penalized
    /// </summary>
    public required float PenaltyStart { get; init; }
    public required float PenaltyEnd { get; init; }

    /// <summary>
    /// Quality penalty (negative value)
    /// </summary>
    public float QualityPenalty { get; init; } = -0.10f;

    /// <summary>
    /// Traits added when mistimed
    /// </summary>
    public List<string> PenaltyTraits { get; init; } = [];

    /// <summary>
    /// Warning message shown
    /// </summary>
    public string? Message { get; init; }
}
```

---

## 🌳 Process Graphs

Recipes define their crafting process as a **directed graph** — sometimes linear, sometimes branching, optionally with cycles.

### Graph Types

| Type | Description | Example |
|------|-------------|---------|
| **Linear** | Fixed sequence of windows | Basic sword: Heat → Hammer → Quench → Grind |
| **Branching** | Choices lead to different outcomes | Blade vs. Armor divergence after shaping |
| **Cyclic** | Repeatable actions (with diminishing/increasing returns) | Polish loop (risk of over-polishing) |
| **Parallel** | Multiple valid actions in same window | Heat OR Anneal at start |

### ProcessGraph

```csharp
public class ProcessGraph
{
    /// <summary>
    /// All nodes in the crafting process
    /// </summary>
    public required List<ProcessNode> Nodes { get; init; }

    /// <summary>
    /// The starting node(s) of the graph
    /// </summary>
    public required List<string> EntryNodeIds { get; init; }

    /// <summary>
    /// Validates the graph structure (no orphans, valid connections)
    /// </summary>
    public GraphValidationResult Validate();
}

public class ProcessNode
{
    /// <summary>
    /// Unique identifier for this node
    /// </summary>
    public required string NodeId { get; init; }

    /// <summary>
    /// The action window available at this node
    /// </summary>
    public required ActionWindow ActionWindow { get; init; }

    /// <summary>
    /// Connections to next possible nodes
    /// </summary>
    public List<ProcessEdge> Edges { get; init; } = [];

    /// <summary>
    /// Whether this node can be the final step
    /// </summary>
    public bool CanFinalize { get; init; } = false;

    /// <summary>
    /// Maximum times this node can be visited (null = unlimited for cycles)
    /// </summary>
    public int? MaxVisits { get; init; } = 1;
}

public class ProcessEdge
{
    /// <summary>
    /// Target node ID
    /// </summary>
    public required string TargetNodeId { get; init; }

    /// <summary>
    /// Condition for this edge (optional)
    /// </summary>
    public EdgeCondition? Condition { get; init; }

    /// <summary>
    /// Quality modifier applied when taking this path
    /// </summary>
    public float QualityModifier { get; init; } = 0f;
}

public class EdgeCondition
{
    /// <summary>
    /// Required context property to take this edge
    /// </summary>
    public string? RequiredProperty { get; init; }

    /// <summary>
    /// Minimum quality required to take this edge
    /// </summary>
    public float? MinQuality { get; init; }

    /// <summary>
    /// Action must have been optimal to take this edge
    /// </summary>
    public bool RequiresOptimal { get; init; } = false;
}
```

### Example: Cyclic Polishing

```
                    ┌─────────────────┐
                    │                 │
                    ▼                 │
┌─────────┐    ┌─────────┐    ┌──────┴──────┐    ┌─────────┐
│  SHAPE  │───►│ POLISH  │───►│ POLISH MORE │───►│ FINISH  │
└─────────┘    └─────────┘    └─────────────┘    └─────────┘
                    │                 │
                    │                 │ (max 3 visits)
                    │                 │
                    └────────OR───────┘
                           │
                           ▼
                    ┌─────────────┐
                    │ OVER-POLISH │ (quality penalty!)
                    └─────────────┘
```

**Rules:**
- `POLISH` node has `MaxVisits: 3`
- Each polish adds +0.05 quality
- 4th polish attempt triggers `OVER-POLISH` with -0.15 quality and "Scratched" trait

---

## 📄 JSON Process Definitions

Processes, action windows, and graphs are defined in JSON to allow new industries without code changes.

### Process Definition Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "industryTag": { "type": "string" },
    "actions": {
      "type": "array",
      "description": "Available actions for this industry (tools/techniques)",
      "items": {
        "type": "object",
        "properties": {
          "tag": { "type": "string" },
          "name": { "type": "string" },
          "description": { "type": "string" },
          "icon": { "type": "string" }
        },
        "required": ["tag", "name"]
      }
    },
    "processes": {
      "type": "array",
      "description": "Crafting processes (graphs) for recipes",
      "items": {
        "type": "object",
        "properties": {
          "processId": { "type": "string" },
          "name": { "type": "string" },
          "description": { "type": "string" },
          "baseProgressRate": { "type": "number" },
          "entryNodeIds": {
            "type": "array",
            "items": { "type": "string" }
          },
          "nodes": {
            "type": "array",
            "items": { "$ref": "#/definitions/processNode" }
          },
          "mistimeEffects": {
            "type": "array",
            "items": { "$ref": "#/definitions/mistimeEffect" }
          }
        },
        "required": ["processId", "name", "entryNodeIds", "nodes"]
      }
    }
  },
  "definitions": {
    "processNode": {
      "type": "object",
      "properties": {
        "nodeId": { "type": "string" },
        "actionTag": { "type": "string" },
        "windowStart": { "type": "number" },
        "windowEnd": { "type": "number" },
        "optimalStart": { "type": "number" },
        "optimalEnd": { "type": "number" },
        "optimalQualityBonus": { "type": "number" },
        "standardQualityBonus": { "type": "number" },
        "optimalTraits": {
          "type": "array",
          "items": { "type": "string" }
        },
        "optimalMessage": { "type": "string" },
        "standardMessage": { "type": "string" },
        "canFinalize": { "type": "boolean" },
        "maxVisits": { "type": "integer" },
        "edges": {
          "type": "array",
          "items": { "$ref": "#/definitions/processEdge" }
        }
      },
      "required": ["nodeId", "actionTag", "windowStart", "windowEnd"]
    },
    "processEdge": {
      "type": "object",
      "properties": {
        "targetNodeId": { "type": "string" },
        "qualityModifier": { "type": "number" },
        "condition": {
          "type": "object",
          "properties": {
            "requiredProperty": { "type": "string" },
            "minQuality": { "type": "number" },
            "requiresOptimal": { "type": "boolean" }
          }
        }
      },
      "required": ["targetNodeId"]
    },
    "mistimeEffect": {
      "type": "object",
      "properties": {
        "actionTag": { "type": "string" },
        "penaltyStart": { "type": "number" },
        "penaltyEnd": { "type": "number" },
        "qualityPenalty": { "type": "number" },
        "penaltyTraits": {
          "type": "array",
          "items": { "type": "string" }
        },
        "message": { "type": "string" }
      },
      "required": ["actionTag", "penaltyStart", "penaltyEnd"]
    }
  },
  "required": ["industryTag", "actions", "processes"]
}
```

### Example: Smithing Industry (Progress Bar Model)

```json
{
  "industryTag": "smithing",
  "actions": [
    { "tag": "heat", "name": "Heat", "description": "Apply heat to the metal", "icon": "flame" },
    { "tag": "hammer", "name": "Hammer", "description": "Strike the metal to shape it", "icon": "hammer" },
    { "tag": "quench", "name": "Quench", "description": "Rapidly cool in water or oil", "icon": "water" },
    { "tag": "temper", "name": "Temper", "description": "Controlled slow cooling", "icon": "thermometer" },
    { "tag": "grind", "name": "Grind", "description": "Sharpen on grinding wheel", "icon": "sparkles" },
    { "tag": "polish", "name": "Polish", "description": "Buff to a shine", "icon": "star" }
  ],
  "processes": [
    {
      "processId": "forge_blade",
      "name": "Forge Blade",
      "description": "Standard blade forging process",
      "baseProgressRate": 2.5,
      "entryNodeIds": ["heat_1"],
      "nodes": [
        {
          "nodeId": "heat_1",
          "actionTag": "heat",
          "windowStart": 0.0,
          "windowEnd": 15.0,
          "optimalStart": 5.0,
          "optimalEnd": 12.0,
          "optimalQualityBonus": 0.10,
          "standardQualityBonus": 0.05,
          "optimalMessage": "The metal glows a perfect orange!",
          "standardMessage": "The metal heats up.",
          "edges": [
            { "targetNodeId": "hammer_1" },
            { "targetNodeId": "fold_1", "condition": { "minQuality": 1.05 } }
          ]
        },
        {
          "nodeId": "fold_1",
          "actionTag": "hammer",
          "windowStart": 15.0,
          "windowEnd": 25.0,
          "optimalStart": 18.0,
          "optimalEnd": 22.0,
          "optimalQualityBonus": 0.15,
          "standardQualityBonus": 0.08,
          "optimalTraits": ["Folded Steel"],
          "optimalMessage": "You fold the steel with expert precision!",
          "maxVisits": 3,
          "edges": [
            { "targetNodeId": "fold_1" },
            { "targetNodeId": "hammer_1" }
          ]
        },
        {
          "nodeId": "hammer_1",
          "actionTag": "hammer",
          "windowStart": 25.0,
          "windowEnd": 45.0,
          "optimalStart": 30.0,
          "optimalEnd": 40.0,
          "optimalQualityBonus": 0.15,
          "standardQualityBonus": 0.05,
          "optimalMessage": "Perfect strikes shape the blade beautifully!",
          "standardMessage": "You hammer the metal into shape.",
          "edges": [
            { "targetNodeId": "hammer_2" },
            { "targetNodeId": "quench_1" }
          ]
        },
        {
          "nodeId": "hammer_2",
          "actionTag": "hammer",
          "windowStart": 45.0,
          "windowEnd": 55.0,
          "optimalStart": 48.0,
          "optimalEnd": 52.0,
          "optimalQualityBonus": 0.10,
          "standardQualityBonus": 0.03,
          "optimalMessage": "Fine adjustments perfect the shape.",
          "edges": [
            { "targetNodeId": "quench_1" },
            { "targetNodeId": "temper_1" }
          ]
        },
        {
          "nodeId": "quench_1",
          "actionTag": "quench",
          "windowStart": 55.0,
          "windowEnd": 70.0,
          "optimalStart": 60.0,
          "optimalEnd": 65.0,
          "optimalQualityBonus": 0.12,
          "standardQualityBonus": 0.05,
          "optimalTraits": ["Hardened"],
          "optimalMessage": "The blade hardens with a satisfying hiss!",
          "standardMessage": "The metal cools rapidly.",
          "edges": [
            { "targetNodeId": "grind_1" }
          ]
        },
        {
          "nodeId": "temper_1",
          "actionTag": "temper",
          "windowStart": 55.0,
          "windowEnd": 70.0,
          "optimalStart": 58.0,
          "optimalEnd": 68.0,
          "optimalQualityBonus": 0.08,
          "standardQualityBonus": 0.04,
          "optimalTraits": ["Well-Tempered"],
          "optimalMessage": "Careful tempering balances the blade.",
          "edges": [
            { "targetNodeId": "grind_1" }
          ]
        },
        {
          "nodeId": "grind_1",
          "actionTag": "grind",
          "windowStart": 75.0,
          "windowEnd": 90.0,
          "optimalStart": 80.0,
          "optimalEnd": 87.0,
          "optimalQualityBonus": 0.10,
          "standardQualityBonus": 0.05,
          "optimalTraits": ["Razor Sharp"],
          "optimalMessage": "A perfect edge emerges!",
          "standardMessage": "You sharpen the blade.",
          "canFinalize": true,
          "edges": [
            { "targetNodeId": "polish_1" }
          ]
        },
        {
          "nodeId": "polish_1",
          "actionTag": "polish",
          "windowStart": 90.0,
          "windowEnd": 100.0,
          "optimalStart": 92.0,
          "optimalEnd": 98.0,
          "optimalQualityBonus": 0.08,
          "standardQualityBonus": 0.03,
          "optimalTraits": ["Mirror Finish"],
          "optimalMessage": "The blade gleams like a mirror!",
          "canFinalize": true,
          "maxVisits": 2,
          "edges": [
            { "targetNodeId": "polish_1" },
            { "targetNodeId": "over_polish", "condition": { "requiredProperty": "polish_count_3" } }
          ]
        },
        {
          "nodeId": "over_polish",
          "actionTag": "polish",
          "windowStart": 90.0,
          "windowEnd": 100.0,
          "optimalQualityBonus": -0.15,
          "standardQualityBonus": -0.15,
          "optimalTraits": ["Scratched"],
          "optimalMessage": "⚠️ You've polished too much! Fine scratches appear.",
          "canFinalize": true
        }
      ],
      "mistimeEffects": [
        {
          "actionTag": "hammer",
          "penaltyStart": 55.0,
          "penaltyEnd": 75.0,
          "qualityPenalty": -0.10,
          "penaltyTraits": ["Stress Marks"],
          "message": "⚠️ The metal is too cool to hammer effectively!"
        },
        {
          "actionTag": "quench",
          "penaltyStart": 0.0,
          "penaltyEnd": 40.0,
          "qualityPenalty": -0.20,
          "penaltyTraits": ["Brittle"],
          "message": "⚠️ Quenching too early makes the metal brittle!"
        },
        {
          "actionTag": "grind",
          "penaltyStart": 0.0,
          "penaltyEnd": 70.0,
          "qualityPenalty": -0.15,
          "message": "⚠️ The metal hasn't been properly treated for grinding!"
        }
      ]
    }
  ]
}
```

### Example: Alchemy Industry (Progress Bar Model)

```json
{
  "industryTag": "alchemy",
  "actions": [
    { "tag": "stir", "name": "Stir", "description": "Stir the mixture", "icon": "spoon" },
    { "tag": "heat", "name": "Heat", "description": "Apply heat to brew", "icon": "flame" },
    { "tag": "cool", "name": "Cool", "description": "Let the mixture cool", "icon": "snowflake" },
    { "tag": "add_reagent", "name": "Add Reagent", "description": "Add next ingredient", "icon": "flask" },
    { "tag": "infuse", "name": "Infuse", "description": "Channel essence into mixture", "icon": "sparkle" }
  ],
  "processes": [
    {
      "processId": "brew_potion",
      "name": "Brew Potion",
      "description": "Standard potion brewing process",
      "baseProgressRate": 3.0,
      "entryNodeIds": ["heat_brew"],
      "nodes": [
        {
          "nodeId": "heat_brew",
          "actionTag": "heat",
          "windowStart": 0.0,
          "windowEnd": 20.0,
          "optimalStart": 8.0,
          "optimalEnd": 15.0,
          "optimalQualityBonus": 0.10,
          "standardQualityBonus": 0.05,
          "optimalMessage": "The mixture reaches the perfect simmer!",
          "edges": [
            { "targetNodeId": "stir_1" }
          ]
        },
        {
          "nodeId": "stir_1",
          "actionTag": "stir",
          "windowStart": 20.0,
          "windowEnd": 35.0,
          "optimalStart": 25.0,
          "optimalEnd": 32.0,
          "optimalQualityBonus": 0.08,
          "standardQualityBonus": 0.03,
          "optimalMessage": "Ingredients blend harmoniously!",
          "maxVisits": 3,
          "edges": [
            { "targetNodeId": "stir_1" },
            { "targetNodeId": "add_reagent_1" }
          ]
        },
        {
          "nodeId": "add_reagent_1",
          "actionTag": "add_reagent",
          "windowStart": 35.0,
          "windowEnd": 50.0,
          "optimalStart": 40.0,
          "optimalEnd": 47.0,
          "optimalQualityBonus": 0.15,
          "standardQualityBonus": 0.08,
          "optimalTraits": ["Potent"],
          "optimalMessage": "The reagent dissolves perfectly!",
          "edges": [
            { "targetNodeId": "stir_2" },
            { "targetNodeId": "infuse_1", "condition": { "minQuality": 1.15 } }
          ]
        },
        {
          "nodeId": "stir_2",
          "actionTag": "stir",
          "windowStart": 50.0,
          "windowEnd": 65.0,
          "optimalStart": 55.0,
          "optimalEnd": 62.0,
          "optimalQualityBonus": 0.05,
          "standardQualityBonus": 0.02,
          "edges": [
            { "targetNodeId": "cool_1" }
          ]
        },
        {
          "nodeId": "infuse_1",
          "actionTag": "infuse",
          "windowStart": 50.0,
          "windowEnd": 70.0,
          "optimalStart": 55.0,
          "optimalEnd": 65.0,
          "optimalQualityBonus": 0.20,
          "standardQualityBonus": 0.10,
          "optimalTraits": ["Essence-Infused"],
          "optimalMessage": "Magical essence binds to the mixture!",
          "edges": [
            { "targetNodeId": "cool_1" }
          ]
        },
        {
          "nodeId": "cool_1",
          "actionTag": "cool",
          "windowStart": 75.0,
          "windowEnd": 95.0,
          "optimalStart": 80.0,
          "optimalEnd": 90.0,
          "optimalQualityBonus": 0.08,
          "standardQualityBonus": 0.03,
          "optimalMessage": "The potion settles into stable form.",
          "canFinalize": true
        }
      ],
      "mistimeEffects": [
        {
          "actionTag": "add_reagent",
          "penaltyStart": 0.0,
          "penaltyEnd": 30.0,
          "qualityPenalty": -0.15,
          "penaltyTraits": ["Unstable"],
          "message": "⚠️ Adding reagent too early causes instability!"
        },
        {
          "actionTag": "stir",
          "penaltyStart": 70.0,
          "penaltyEnd": 100.0,
          "qualityPenalty": -0.10,
          "message": "⚠️ Stirring during cooling disrupts the mixture!"
        }
      ]
    }
  ]
}
```

### Loading Processes at Runtime

```csharp
public interface IProcessLoader
{
    /// <summary>
    /// Loads process definitions from JSON for an industry
    /// </summary>
    IndustryProcessDefinition LoadFromJson(string json);

    /// <summary>
    /// Loads process definitions from a file path
    /// </summary>
    IndustryProcessDefinition LoadFromFile(string filePath);
}

public class IndustryProcessDefinition
{
    public required string IndustryTag { get; init; }
    public required List<ActionDefinition> Actions { get; init; }
    public required List<ProcessDefinition> Processes { get; init; }
}

public class ActionDefinition
{
    public required string Tag { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Icon { get; init; }
}

public class ProcessDefinition
{
    public required string ProcessId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public float BaseProgressRate { get; init; } = 1.0f;
    public required List<string> EntryNodeIds { get; init; }
    public required List<ProcessNodeDefinition> Nodes { get; init; }
    public List<MistimeEffectDefinition> MistimeEffects { get; init; } = [];
}

public class ProcessNodeDefinition
{
    public required string NodeId { get; init; }
    public required string ActionTag { get; init; }
    public required float WindowStart { get; init; }
    public required float WindowEnd { get; init; }
    public float? OptimalStart { get; init; }
    public float? OptimalEnd { get; init; }
    public float OptimalQualityBonus { get; init; } = 0.1f;
    public float StandardQualityBonus { get; init; } = 0.05f;
    public List<string> OptimalTraits { get; init; } = [];
    public string? OptimalMessage { get; init; }
    public string? StandardMessage { get; init; }
    public bool CanFinalize { get; init; } = false;
    public int? MaxVisits { get; init; }
    public List<ProcessEdgeDefinition> Edges { get; init; } = [];
}

public class ProcessEdgeDefinition
{
    public required string TargetNodeId { get; init; }
    public float QualityModifier { get; init; } = 0f;
    public EdgeConditionDefinition? Condition { get; init; }
}

public class EdgeConditionDefinition
{
    public string? RequiredProperty { get; init; }
    public float? MinQuality { get; init; }
    public bool RequiresOptimal { get; init; } = false;
}

public class MistimeEffectDefinition
{
    public required string ActionTag { get; init; }
    public required float PenaltyStart { get; init; }
    public required float PenaltyEnd { get; init; }
    public float QualityPenalty { get; init; } = -0.1f;
    public List<string> PenaltyTraits { get; init; } = [];
    public string? Message { get; init; }
}
```

---

## 🚀 Implementation Roadmap

### Phase 1: Core Abstractions
- [ ] `CraftingProgress` — Progress bar state (0.0 → 100.0)
- [ ] `CraftingContext` — Session state container with quality/traits
- [ ] `ActionWindow` — Defines timing windows for actions
- [ ] `TimedAction` — Record of action applied at specific progress
- [ ] `ActionResult` — Outcome of applying an action (optimal/standard/mistimed)

### Phase 2: Process Graph
- [ ] `ProcessGraph` — Container for nodes and edges
- [ ] `ProcessNode` — Graph node with action window and edges
- [ ] `ProcessEdge` — Connection between nodes with conditions
- [ ] `EdgeCondition` — Requirements to traverse an edge
- [ ] `GraphValidationResult` — Validate graph structure

### Phase 3: JSON Loading
- [ ] `ProcessDefinition` — Data class for JSON deserialization
- [ ] `ProcessNodeDefinition` — Node JSON structure
- [ ] `MistimeEffectDefinition` — Penalty definitions
- [ ] `IProcessLoader` — Load definitions from JSON files
- [ ] `IProcessRegistry` — Lookup processes by ID
- [ ] Validation logic (orphan nodes, invalid edges)

### Phase 4: Crafting Session Engine
- [ ] `CraftingSession` — Orchestrates progress advancement
- [ ] `ICraftingEngine` — Core loop: advance progress, evaluate actions
- [ ] Mistime detection and penalty application
- [ ] Node visit tracking (for cycle limits)
- [ ] Finalization logic with quality/trait calculation

### Phase 5: Knowledge Integration
- [ ] `IKnowledgeEnhancer` — Knowledge-based bonuses
- [ ] Wider optimal windows for knowledgeable crafters
- [ ] Reduced mistime penalties
- [ ] Bonus traits from expertise

### Phase 6: GUI Integration Hooks
- [ ] `GetCurrentNode()` — Which node is active
- [ ] `GetAvailableActions()` — Actions valid at current progress
- [ ] `GetOptimalWindow()` — Highlight optimal timing range
- [ ] `PreviewAction()` — Show potential outcome before committing
- [ ] Progress bar tick events for UI updates

---

## 📝 Design Decisions Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2025-12-16 | Use `CraftingStep` not `ProcessStep` | Consistency with existing `CraftingResult` |
| 2025-12-16 | Create `Crafting/` subfolder | Crafting is coupled to Industries but complex enough for separation |
| 2025-12-16 | Steps discoverable, Knowledge enhances | Encourages experimentation while rewarding investment |
| 2025-12-16 | JSON-defined steps and modifiers | New industries without code changes; designers can iterate independently |
| 2025-12-16 | Progress bar model (0.0 → 100.0) | Fantasy Life-inspired timing minigame; actions at right moment improve quality |
| 2025-12-16 | Process graphs (linear/branching/cyclic) | Flexible recipe structures; polish cycles, forge vs armor branches |
| 2025-12-16 | Optimal windows within action windows | Skill expression through timing; rewards mastery without hard gates |
| 2025-12-16 | Auto-craft baseline by skill tier | Passive players still succeed; engaged players get bonuses; never punishing |
| 2025-12-16 | Misapplied actions only penalize on action | Doing nothing is safe; penalties require active mistakes |

---

## 🔮 Future Considerations

1. **Tool Requirements** — Some steps may require specific tools (anvil, forge, etc.)
2. **Environmental Factors** — Location/time of day affecting outcomes
3. **Collaborative Crafting** — Multiple characters contributing steps
4. **Failure Recovery** — Partial salvage when crafting fails
5. **Step Variants** — Same step tag with different implementations (e.g., "quench_water" vs "quench_oil")
