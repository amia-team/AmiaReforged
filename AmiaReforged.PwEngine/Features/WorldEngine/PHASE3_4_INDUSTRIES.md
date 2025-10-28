# Phase 3.4: Industries CQRS Implementation
**Started**: October 28, 2025
**Status**: 🟡 In Progress - 60% Complete

> **See detailed progress**: [PHASE3_4_INDUSTRIES_PROGRESS.md](PHASE3_4_INDUSTRIES_PROGRESS.md)

---

## Overview

Industries represent fields of knowledge and crafting expertise (Blacksmith, Alchemist, Scholar, Mercenary, etc.). Characters learn from industries through guilds or player teachers using knowledge points.

This phase adds **Recipe/Reaction** system to Industries with CQRS patterns for managing crafting processes.

---

## Domain Understanding

### Core Concepts

1. **Industry**: Category of expertise (e.g., Blacksmithing, Alchemy)
2. **Recipe/Reaction**: A crafting process with inputs → outputs (like Dwarf Fortress)
3. **Ingredients**: Required items/resources to craft
4. **Products**: Resulting items from successful crafting
5. **IndustryMembership**: Character's participation in an industry with proficiency level
6. **Knowledge**: Learnable skills within an industry

### Key Insight
> The **process** between inputs and outputs can vary per industry (different validation, time, failure modes), but the **data structure** (Recipe) and **CQRS operations** (add/remove/update recipes, craft items) can be standardized.

---

## CQRS Design

### Commands

#### Recipe Management
- `AddRecipeToIndustryCommand` - Add new recipe to an industry
- `RemoveRecipeFromIndustryCommand` - Remove recipe from industry
- `UpdateRecipeCommand` - Modify recipe ingredients/products/requirements

#### Crafting
- `CraftItemCommand` - Execute a recipe to create items
  - Validates: Character has required knowledge, ingredients available, proficiency level
  - Process: Can vary per industry (customizable via handlers)
  - Result: Creates products, consumes ingredients, awards XP

#### Membership (Already exists, will enhance)
- `JoinIndustryCommand`
- `LeaveIndustryCommand`
- `LearnKnowledgeCommand`

### Queries

- `GetIndustryRecipesQuery` - Get all recipes for an industry
- `GetAvailableRecipesQuery` - Get recipes a character can craft (has knowledge + proficiency)
- `GetRecipeDetailsQuery` - Get specific recipe details
- `GetIndustryMembershipQuery` - Get character's industry membership
- `GetCharacterIndustriesQuery` - Get all industries a character participates in

---

## Value Objects

### New Types
```csharp
RecipeId - Strong type for recipe identifiers
Ingredient - (ItemDefinition, Quantity, Quality requirements)
Product - (ItemDefinition, Quantity, Quality output)
```

### Existing Types (Reuse)
- `IndustryTag` - Identifies industries
- `CharacterId` - Character identity
- `Quantity` - Item amounts
- `ProficiencyLevel` - Skill levels

---

## Implementation Plan

### Step 1: Value Objects ✅ (Check existing)
- [x] IndustryTag exists
- [x] CharacterId exists
- [x] Quantity exists
- [ ] Create RecipeId
- [ ] Create Ingredient record
- [ ] Create Product record

### Step 2: Domain Models
- [ ] Create Recipe entity
- [ ] Add Recipes collection to Industry
- [ ] Create CraftingResult

### Step 3: BDD Tests (Code-First)
- [ ] Test: Add recipe to industry
- [ ] Test: Remove recipe from industry
- [ ] Test: Character crafts item with sufficient knowledge
- [ ] Test: Character cannot craft without required knowledge
- [ ] Test: Character cannot craft without ingredients
- [ ] Test: Successful crafting consumes ingredients and produces items
- [ ] Test: Query available recipes for character

### Step 4: Commands & Handlers
- [ ] Implement AddRecipeToIndustryCommand + Handler
- [ ] Implement RemoveRecipeFromIndustryCommand + Handler
- [ ] Implement UpdateRecipeCommand + Handler
- [ ] Implement CraftItemCommand + Handler (with extensible process)

### Step 5: Queries & Handlers
- [ ] Implement GetIndustryRecipesQuery + Handler
- [ ] Implement GetAvailableRecipesQuery + Handler
- [ ] Implement GetRecipeDetailsQuery + Handler

### Step 6: Repository Updates
- [ ] Add recipe storage to IIndustryRepository
- [ ] Implement recipe persistence in PersistentIndustryRepository (if needed)
- [ ] Update InMemoryIndustryRepository with recipe support

### Step 7: Integration
- [ ] Wire up command/query handlers to WorldEngine
- [ ] Add industry recipe loading from JSON
- [ ] Create sample industry definitions with recipes

---

## Success Criteria

- [ ] All BDD tests pass
- [ ] Recipes can be added/removed/updated via commands
- [ ] Characters can craft items via CraftItemCommand
- [ ] Queries return correct available recipes based on character knowledge
- [ ] Recipe system is extensible (different industries can customize crafting logic)
- [ ] Integration tests demonstrate full workflow
- [ ] No primitive obsession - all IDs and values use strong types

---

## Example Recipe Structure (JSON)

```json
{
  "industryTag": "blacksmithing",
  "recipes": [
    {
      "recipeId": "iron_sword",
      "name": "Iron Sword",
      "requiredKnowledge": ["basic_forging"],
      "requiredProficiency": "Novice",
      "ingredients": [
        { "itemResRef": "iron_ingot", "quantity": 3, "minQuality": 1 },
        { "itemResRef": "leather_strip", "quantity": 1, "minQuality": 1 }
      ],
      "products": [
        { "itemResRef": "iron_sword", "quantity": 1, "quality": 2 }
      ],
      "craftingTime": 300,
      "knowledgePointsAwarded": 5
    }
  ]
}
```

---

## Notes

- Crafting **process** can be customized per industry via strategy pattern or event handlers
- Core CQRS operations remain standard across all industries
- Recipe validation logic can vary (e.g., alchemy might check temperature, blacksmithing checks tool quality)
- This design supports future expansion to multi-step crafting, quality variation, critical success/failure

---

**Next Steps**: Create BDD tests for recipe management, then implement commands and handlers.

