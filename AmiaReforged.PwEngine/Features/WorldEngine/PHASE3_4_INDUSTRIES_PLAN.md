# Phase 3.4: Industries CQRS Implementation
**Status**: 🚀 In Progress  
**Started**: October 28, 2025
---
## Goal
Apply CQRS pattern to the Industries subsystem. Industries are knowledge categories (Blacksmithing, Alchemy, Scholar, etc.) that contain recipes/reactions. People learn from industries via guilds or player teachers.
---
## Current Industries System
Industries are knowledge domains containing:
- **Recipes/Reactions** - Input ingredients → Output products
- **Knowledge** - Learned by personas through teaching or guilds
- **Membership** - Personas can belong to multiple industries
- **Proficiency Levels** - Track skill advancement
Key insight: **Processes between inputs and outputs vary by game/application layer**, so CQRS focuses on recipe/reaction management and knowledge transfer.
---
## Planned Commands
### Recipe/Reaction Management
1. **AddRecipeToIndustryCommand**
   - Add a new recipe/reaction to an industry
   - Validates recipe structure (inputs, outputs, duration)
   - Publishes `RecipeAddedEvent`
2. **RemoveRecipeFromIndustryCommand**
   - Remove a recipe from an industry
   - Validates recipe exists
   - Publishes `RecipeRemovedEvent`
3. **UpdateRecipeCommand**
   - Update recipe details (inputs, outputs, duration, requirements)
   - Validates changes
   - Publishes `RecipeUpdatedEvent`
### Knowledge Transfer
4. **LearnKnowledgeCommand**
   - Persona learns knowledge from industry
   - Requires teacher (player/guild) or knowledge points
   - Updates proficiency
   - Publishes `KnowledgeLearnedEvent`
5. **TeachKnowledgeCommand**
   - Persona teaches knowledge to another
   - Validates teacher has knowledge
   - Transfers knowledge points
   - Publishes `KnowledgeTaughtEvent`
### Crafting (Generic)
6. **CraftItemCommand**
   - Persona crafts item using a recipe
   - Validates recipe knowledge
   - Consumes ingredients
   - Produces output (process varies by implementation)
   - Publishes `ItemCraftedEvent`
### Membership
7. **JoinIndustryCommand**
   - Persona joins industry (guild membership)
   - Records membership
   - Publishes `IndustryJoinedEvent`
8. **LeaveIndustryCommand**
   - Persona leaves industry
   - Publishes `IndustryLeftEvent`
---
## Planned Queries
### Recipe Queries
1. **GetIndustryRecipesQuery**
   - Get all recipes for an industry
   - Returns list of RecipeDto
2. **GetRecipeDetailsQuery**
   - Get detailed recipe info (inputs, outputs, requirements)
   - Returns RecipeDetailsDto
3. **GetCraftableRecipesQuery**
   - Get recipes persona can craft (has knowledge + ingredients)
   - Returns list of CraftableRecipeDto
### Knowledge Queries
4. **GetPersonaKnowledgeQuery**
   - Get all knowledge a persona has learned
   - Returns list of KnowledgeDto with proficiency levels
5. **GetIndustryKnowledgeQuery**
   - Get all knowledge available in an industry
   - Returns list of AvailableKnowledgeDto
### Membership Queries
6. **GetIndustryMembersQuery**
   - Get all members of an industry
   - Returns list of MembershipDto
7. **GetPersonaIndustriesQuery**
   - Get all industries a persona belongs to
   - Returns list of IndustryMembershipDto
---
## Domain Events
### Recipe Events
1. **RecipeAddedEvent** - Recipe added to industry
2. **RecipeRemovedEvent** - Recipe removed from industry
3. **RecipeUpdatedEvent** - Recipe modified
### Knowledge Events
4. **KnowledgeLearnedEvent** - Persona learned knowledge
5. **KnowledgeTaughtEvent** - Persona taught another
6. **ProficiencyIncreasedEvent** - Skill level increased
### Crafting Events
7. **ItemCraftedEvent** - Item crafted via recipe
8. **CraftingFailedEvent** - Crafting attempt failed
### Membership Events
9. **IndustryJoinedEvent** - Joined industry/guild
10. **IndustryLeftEvent** - Left industry/guild
---
## Value Objects
- `IndustryCode` ✅ (already exists)
- `RecipeId` - Unique recipe identifier
- `KnowledgeId` - Unique knowledge identifier
- `ProficiencyLevel` ✅ (already exists)
- `Duration` - Crafting/learning duration
- (Reuse: PersonaId, Quantity)
---
## DTOs
### Recipe DTOs
- `RecipeDto` - Basic recipe info
- `RecipeDetailsDto` - Full recipe with inputs/outputs
- `CraftableRecipeDto` - Recipe + can craft status
### Knowledge DTOs
- `KnowledgeDto` - Learned knowledge with proficiency
- `AvailableKnowledgeDto` - Knowledge available to learn
### Membership DTOs
- `MembershipDto` - Guild membership info
- `IndustryMembershipDto` - Industry participation
---
## Implementation Plan
### Phase 1: Recipe Management (4-5 hours)
1. **AddRecipeToIndustryCommand** + tests
2. **RemoveRecipeFromIndustryCommand** + tests
3. **UpdateRecipeCommand** + tests
4. Events for all mutations
5. Recipe queries + tests
### Phase 2: Knowledge System (3-4 hours)
1. **LearnKnowledgeCommand** + tests
2. **TeachKnowledgeCommand** + tests
3. Knowledge queries + tests
4. Proficiency tracking
### Phase 3: Crafting (Generic) (3-4 hours)
1. **CraftItemCommand** + tests
2. Recipe validation
3. Ingredient consumption
4. Event publishing
5. Note: Actual crafting process varies by implementation
### Phase 4: Membership (2 hours)
1. **JoinIndustryCommand** + tests
2. **LeaveIndustryCommand** + tests
3. Membership queries
### Phase 5: Integration Tests (2-3 hours)
1. Full workflow: Join → Learn → Craft
2. Teaching scenarios
3. Recipe lifecycle
4. Event ordering
**Total Estimated**: 14-18 hours
---
## Repository Interfaces
### Existing
- `IIndustryRepository` - Industry definitions
- `IIndustryMembershipRepository` - Membership tracking
- `ICharacterKnowledgeRepository` - Knowledge learned
### May Need
- `IRecipeRepository` - Recipe storage/retrieval
- `ICraftingRepository` - Active crafting operations (if needed)
---
## Key Design Decisions
### 1. Recipes vs Reactions
Using "Recipe" as the general term, but supporting "Reaction" concept:
- **Inputs** (ingredients/materials)
- **Outputs** (products)
- **Process** (varies by implementation - can be crafting, alchemy, research, etc.)
### 2. Crafting Process Abstraction
`CraftItemCommand` is generic:
- Validates recipe knowledge
- Consumes inputs
- **Delegates actual process to application layer**
- Publishes events
Different industries can have different implementations:
- Blacksmithing: Physical crafting with skill checks
- Alchemy: Chemical reactions with failure chances
- Scholarship: Research with time investment
### 3. Knowledge Points vs Gold
Knowledge transfer can require:
- Knowledge points (learning currency)
- Gold (payment for teaching)
- Both (configurable)
Commands handle the transfer, integration layer handles pricing.
---
## Success Criteria
- [ ] Recipe management commands implemented (add, remove, update)
- [ ] Knowledge transfer commands implemented (learn, teach)
- [ ] Generic crafting command implemented
- [ ] Membership commands implemented
- [ ] All queries implemented
- [ ] Events published for all mutations
- [ ] 60+ tests passing
- [ ] Integration tests complete
- [ ] Documentation complete
---
## Pattern from Phase 3.3
Following proven pattern:
1. **BDD tests first** (Given-When-Then)
2. **Factory method validation**
3. **Business rules in handlers**
4. **Events after mutations**
5. **Read-only queries with DTOs**
6. **Process abstraction** - Commands handle structure, app layer handles specifics
---
**Starting with Recipe Management (AddRecipeToIndustryCommand)!**
---
**Last Updated**: October 28, 2025
