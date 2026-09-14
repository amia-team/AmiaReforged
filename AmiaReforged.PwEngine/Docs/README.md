# PwEngine Documentation Portal

All markdown docs now live flat in `AmiaReforged.PwEngine/Docs/`. This file is the index.

## Start here

| Doc | What |
| --- | --- |
| `PwEngine-README.md` | Project overview, Features/Database/Tests layout (was `../README.md`) |
| `AgentsInstructions.md` | Agent working instructions (was `../AgentsInstructions.md`) |
| `WorldEngine-docs-README.md` | WorldEngine entry point |
| `WorldEngine-architecture.md` | WorldEngine architecture |
| `WorldEngine-subsystems.md` | Subsystem catalogue + facade shape |
| `WorldEngine-cqrs.md` | Command/Query/Event conventions |
| `WorldEngine-api-reference.md` | HTTP API reference |
| `WorldEngine-sanitization.md` | Input sanitization rules |

## WorldEngine subsystems

| System | Docs | Code |
| --- | --- | --- |
| Economy (banking, storage, NPC shops, player stalls, property) | `PlayerStalls-RENT_COMMAND_TESTS_COMPLETE.md` | `../Features/WorldEngine/Subsystems/Economy/` |
| Industries / Crafting | `Industries-Crafting-README.md` (process-graph design + Phases 1–6 roadmap) | `../Features/WorldEngine/Subsystems/Industries/` · `../Features/WorldEngine/Application/Industries/` · `../Features/WorldEngine/API/Controllers/` |
| Harvesting | code + tests | `../Features/WorldEngine/Subsystems/Harvesting/` |
| ResourceNodes | `WorldEngine-Resources-NOTICE.md` · node JSON was `../Resources/WorldEngine/Nodes/{Ore,Geodes,Trees}/` | `../Features/WorldEngine/Subsystems/ResourceNodes/` |
| Codex (journal, quests, lore, reputation) | `WorldEngine-codex.md` (has Capability \| Status table) | `../Features/WorldEngine/Subsystems/Codex/` |
| Characters + Personas | `Personas-README.md` | `../Features/WorldEngine/Subsystems/Characters/` · `../Features/WorldEngine/Core/Personas/` |
| Organizations | — | `../Features/WorldEngine/Subsystems/Organizations/` |
| Regions / Areas | — | `../Features/WorldEngine/Subsystems/Regions/` · `AreaGraph/` · `AreaPersistence/` · `../Resources/WorldEngine/Regions/` |
| Traits | — | `../Features/WorldEngine/Subsystems/Traits/` |
| Items | — | `../Features/WorldEngine/Subsystems/Items/` · `../Features/WorldEngine/Application/Items/` |
| Dialogue | — | `../Features/WorldEngine/Subsystems/Dialogue/` |
| Interactions | — | `../Features/WorldEngine/Subsystems/Interactions/` |
| Time | — | `../Features/WorldEngine/Subsystems/Time/` |

## WorldEngine how-tos

| Task | Doc |
| --- | --- |
| Use the facade | `WorldEngine-example-using-the-facade.md` |
| Add a subsystem | `WorldEngine-example-adding-a-subsystem.md` |
| Add a command | `WorldEngine-example-adding-a-command.md` |
| Add a query | `WorldEngine-example-adding-a-query.md` |
| Add a controller | `WorldEngine-example-adding-a-controller.md` |
| Call the HTTP API | `WorldEngine-example-calling-the-api.md` |
| Subscribe to events | `WorldEngine-example-subscribing-to-events.md` |

## Other Features (outside WorldEngine)

| System | Docs | Code |
| --- | --- | --- |
| AI | `AI_REMASTER.md`, `AI_PHASE1_PROGRESS.md`, `AI_PHASE2_PROGRESS.md` | `../Features/AI/` |
| Aetharn minigame | `AETHARN_DESIGN.md` | `../Features/MiniGame/` |
| Rename service (PlayerTools) | `README_RENAME_SERVICE.md` | `../Features/Player/` |
| CharacterTools, Chat, Crafting (legacy), DependencyGraph, DungeonMaster, Encounters, Glyph, Module, NwObjectHelpers, Shutdown, Trap, WindowingSystem | — no dedicated docs, see code | `../Features/<Name>/` |

## Related (repo root, outside PwEngine)

| Doc | What |
| --- | --- |
| `../../WorldSimulator/SimulatorRequirements.md` | Simulation service requirements, In/Out of scope |
| `../../WorldSimulator/TestingPath.md` | Simulator test paths |
| `../../CODEX_STATUS.md`, `../../CODEX_FOUNDATION_COMPLETE.md` | Codex layer status |
| `../../VALUE_OBJECTS_SUCCESS.md` | Value-objects migration notes |

## Original locations (for git history)

| Now | Was |
| --- | --- |
| `AgentsInstructions.md` | `../AgentsInstructions.md` |
| `PwEngine-README.md` | `../README.md` |
| `AI_REMASTER.md`, `AI_PHASE1_PROGRESS.md`, `AI_PHASE2_PROGRESS.md` | `../Features/AI/` |
| `AETHARN_DESIGN.md` | `../Features/MiniGame/Aetharn/` |
| `README_RENAME_SERVICE.md` | `../Features/Player/PlayerTools/Services/` |
| `Personas-README.md` | `../Features/WorldEngine/Core/Personas/README.md` |
| `WorldEngine-*.md` | `../Features/WorldEngine/docs/` + `examples/` |
| `PlayerStalls-RENT_COMMAND_TESTS_COMPLETE.md` | `../Features/WorldEngine/Subsystems/Economy/Tests/Shops/PlayerStalls/` |
| `Industries-Crafting-README.md` | `../Features/WorldEngine/Subsystems/Industries/Crafting/README.md` |
| `WorldEngine-Resources-NOTICE.md` | `../Resources/WorldEngine/NOTICE.md` |
