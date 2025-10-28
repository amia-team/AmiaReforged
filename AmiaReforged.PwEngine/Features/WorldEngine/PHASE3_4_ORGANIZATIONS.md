# Phase 3.4: Organizations CQRS Implementation
**Started**: October 28, 2025
**Status**: 🟢 In Progress

---

## Overview

Organizations are **first-class Personas** in the world system - they can own resources, have reputations, govern industries, and engage in diplomacy. This phase implements CQRS patterns for organization management.

---

## Domain Understanding

### Core Identity
**Organizations ARE Personas**:
```csharp
OrganizationPersona : Persona
  ├── PersonaId (derived from OrganizationId)
  ├── OrganizationId (the "real" ID)
  ├── DisplayName
  └── Type = PersonaType.Organization
```

### Organization Types
- **Faction**: Political groups, factions
- **Settlement**: Towns, cities, villages
- **Government**: Ruling bodies
- **Religion**: Religious organizations
- **Guild**: Craft/trade guilds (govern Industries!)
- **SocialGroup**: Clubs, societies
- **Enterprise**: Businesses, companies

### Current Implementation
```csharp
Organization
  ├── OrganizationId
  ├── Name & Description
  ├── OrganizationType
  ├── ParentOrganization (hierarchy support)
  ├── Inbox (OrganizationRequest queue)
  └── Event: OnRequestMade
```

### Request System
Current primitive-based request handling:
- `OrganizationRequest` - Join, Leave, Promote, Demote, Withdraw, Kick
- `OrganizationResponse` - Sent, Failed, Blocked
- Inbox pattern for async processing

---

## CQRS Design

### Commands

#### Core Membership
- `CreateOrganizationCommand` - Found new organization
- `AddMemberCommand` - Add character to organization
- `RemoveMemberCommand` - Remove/kick member
- `LeaveOrganizationCommand` - Voluntary departure

#### Role Management
- `AssignRoleCommand` - Grant role to member
- `RevokeRoleCommand` - Remove role from member
- `PromoteMemberCommand` - Increase rank
- `DemoteMemberCommand` - Decrease rank

#### Organization Lifecycle
- `DissolveOrganizationCommand` - Shut down organization
- `TransferLeadershipCommand` - Change leader
- `UpdateOrganizationCommand` - Edit name, description

#### Hierarchy & Relations
- `SetParentOrganizationCommand` - Establish hierarchy
- `EstablishDiplomaticRelationCommand` - Ally/rival/neutral

### Queries

#### Basic Info
- `GetOrganizationDetailsQuery` - Full organization info
- `GetOrganizationsByTypeQuery` - Filter by type
- `GetOrganizationHierarchyQuery` - Parent + children

#### Membership
- `GetOrganizationMembersQuery` - Member roster
- `GetCharacterOrganizationsQuery` - What orgs is character in?
- `GetMemberRolesQuery` - What roles does member have?
- `GetMembersWithRoleQuery` - Who has specific role?

#### Relations
- `GetDiplomaticRelationsQuery` - All relations for org
- `GetAlliedOrganizationsQuery` - Filter allies
- `GetRivalOrganizationsQuery` - Filter rivals

---

## Value Objects

### New Types
```csharp
MemberRole - Enum or string-based role system
OrganizationRank - Hierarchy level (Leader, Officer, Member, Recruit)
DiplomaticStance - Alliance, Neutral, Rivalry, War
MembershipStatus - Active, Inactive, Banned, Pending
```

### Existing Types (Reuse)
- `OrganizationId` ✅ Already exists
- `PersonaId` ✅ Organizations have them
- `CharacterId` ✅ For membership
- `OrganizationType` ✅ Already defined

---

## Domain Models

### New Entities
```csharp
OrganizationMember
  ├── CharacterId
  ├── OrganizationId
  ├── Rank
  ├── Roles[]
  ├── JoinedDate
  ├── Status
  └── Metadata

DiplomaticRelation
  ├── OrganizationId (source)
  ├── TargetOrganizationId
  ├── Stance
  ├── EstablishedDate
  └── Treaties[]
```

### Enhanced Organization
Keep existing Organization but add:
- Members collection
- DiplomaticRelations collection
- Metadata for extensibility

---

## Implementation Plan

### Step 1: Value Objects ✅ (Check existing)
- [x] OrganizationId exists
- [x] CharacterId exists
- [ ] Create MemberRole
- [ ] Create OrganizationRank
- [ ] Create DiplomaticStance
- [ ] Create MembershipStatus

### Step 2: Domain Models
- [ ] Create OrganizationMember entity
- [ ] Create DiplomaticRelation entity
- [ ] Enhance Organization with Members/Relations

### Step 3: BDD Tests (Code-First)
- [ ] Test: Create organization (becomes Persona)
- [ ] Test: Add member to organization
- [ ] Test: Remove member
- [ ] Test: Assign/revoke roles
- [ ] Test: Query members
- [ ] Test: Establish diplomatic relations
- [ ] Test: Hierarchy (parent/child orgs)

### Step 4: Commands & Handlers
- [ ] CreateOrganizationCommand + Handler
- [ ] AddMemberCommand + Handler
- [ ] RemoveMemberCommand + Handler
- [ ] AssignRoleCommand + Handler
- [ ] EstablishDiplomaticRelationCommand + Handler

### Step 5: Queries & Handlers
- [ ] GetOrganizationDetailsQuery + Handler
- [ ] GetOrganizationMembersQuery + Handler
- [ ] GetCharacterOrganizationsQuery + Handler
- [ ] GetDiplomaticRelationsQuery + Handler

### Step 6: Repository Updates
- [ ] Add member storage to organization repo
- [ ] Add diplomatic relations storage
- [ ] Update persistence layer

### Step 7: Integration
- [ ] Wire up to WorldEngine
- [ ] Ensure Persona integration works
- [ ] Link to Industries (guilds govern industries)
- [ ] Link to Economy (orgs can own resources)

---

## Key Challenges

### 1. Persona Integration
Organizations must maintain PersonaId consistency:
```csharp
Organization.Id = OrganizationId(Guid)
OrganizationPersona.Id = PersonaId.FromOrganization(OrganizationId)
```

**Solution**: Commands create both Organization AND OrganizationPersona atomically

### 2. Membership vs. Character
Characters belong to multiple organizations:
- Need to track which org they're querying/acting on
- Permission checks vary by org
- Roles are org-specific

**Solution**: OrganizationMember as bridge entity

### 3. Hierarchical Organizations
Parent/child relationships (e.g., Empire → Provinces → Cities):
- Permissions may cascade
- Resources may flow up/down
- Diplomatic relations may inherit

**Solution**: Recursive queries, separate hierarchy commands

### 4. Request System Migration
Current inbox-based async system needs CQRS integration:
- Keep event-driven nature
- Replace primitives with commands
- Maintain async approval workflow

**Solution**: Commands can be "pending" via state machine

---

## Success Criteria

- [ ] All tests pass
- [ ] Organizations can be created via commands
- [ ] Members can join/leave organizations
- [ ] Roles can be assigned/revoked
- [ ] Queries return correct membership data
- [ ] OrganizationPersona created automatically
- [ ] PersonaId consistency maintained
- [ ] Diplomatic relations work
- [ ] Hierarchy queries function correctly
- [ ] No primitive obsession - all IDs are strong types
- [ ] Integration with Industries (guilds)
- [ ] Integration with Economy (resource ownership)

---

## Example JSON Structure (Future)

```json
{
  "organizationId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "Blacksmith Guild",
  "type": "Guild",
  "description": "Association of skilled blacksmiths",
  "members": [
    {
      "characterId": "...",
      "rank": "Leader",
      "roles": ["Guildmaster", "Treasurer"],
      "joinedDate": "2025-01-01"
    }
  ],
  "diplomaticRelations": [
    {
      "targetOrganization": "...",
      "stance": "Alliance",
      "treaties": ["Trade Agreement", "Mutual Defense"]
    }
  ],
  "parentOrganization": null,
  "metadata": {
    "governsIndustry": "blacksmithing",
    "headquarters": "Cordor_City"
  }
}
```

---

## Notes

- Organizations as Personas is CRITICAL - they participate in economy, reputation, etc.
- Guild → Industry linkage will enable recipe teaching, material supply chains
- Government → Region linkage enables territory control
- Hierarchical orgs (Empire → Kingdom → Duchy) need careful design
- Request system can evolve into workflow/approval commands

---

**Next Steps**: Create value objects, then BDD tests for core membership operations.

