# Phase 3.4: Organizations - TODO
**Date**: October 28, 2025
**Status**: 🟡 In Progress - Compilation Issues & Missing Implementations

---

## 🔴 CRITICAL - Fix Compilation Errors

### 1. Repository Interface Mismatches
**Problem**: Multiple `IOrganizationRepository` definitions causing conflicts

**Files Affected**:
- `/Features/WorldEngine/Application/Organizations/Commands/CreateOrganizationCommand.cs` (defines interface)
- `/Features/WorldEngine/Application/Organizations/Queries/GetOrganizationDetailsQuery.cs` (expects interface)
- Other command/query files

**What's Needed**:
```csharp
// Create single source of truth:
// /Features/WorldEngine/Organizations/IOrganizationRepository.cs

using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Database.Entities;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Organizations;

public interface IOrganizationRepository
{
    void Add(IOrganization organization);
    IOrganization? GetById(OrganizationId id); // Uses SharedKernel.OrganizationId
    List<IOrganization> GetAll();
    List<IOrganization> GetByType(OrganizationType type);
    void Update(IOrganization organization);
    void SaveChanges();
}
```

**Action Items**:
- [ ] Remove duplicate interface definitions from command/query files
- [ ] Create single `IOrganizationRepository.cs` in Organizations folder
- [ ] Update all using statements to reference it

### 2. IOrganizationMemberRepository Missing
**Problem**: Interface defined in `AddMemberCommand.cs` but needs to be in proper location

**What's Needed**:
```csharp
// Create: /Features/WorldEngine/Organizations/IOrganizationMemberRepository.cs

using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Organizations;

public interface IOrganizationMemberRepository
{
    void Add(OrganizationMember member);
    OrganizationMember? GetByCharacterAndOrganization(CharacterId characterId, OrganizationId organizationId);
    List<OrganizationMember> GetByOrganization(OrganizationId organizationId);
    List<OrganizationMember> GetByCharacter(CharacterId characterId);
    void Update(OrganizationMember member);
    void Remove(OrganizationMember member);
    void SaveChanges();
}
```

**Action Items**:
- [ ] Extract interface from AddMemberCommand.cs
- [ ] Create proper repository interface file
- [ ] Update all command/query files to use it

### 3. IPersonaRepository Missing Import
**Problem**: `CreateOrganizationCommand.cs` uses `IPersonaRepository` but doesn't have proper using

**Fix**:
```csharp
// Add to CreateOrganizationCommand.cs
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
```

**Action Item**:
- [ ] Check if IPersonaRepository exists in SharedKernel
- [ ] Add proper using statement

### 4. Namespace Ambiguity: Organization
**Problem**: Two `Organization` types exist:
- `AmiaReforged.PwEngine.Database.Entities.Organization`
- `AmiaReforged.PwEngine.Features.WorldEngine.Organizations.Organization`

**Current Workaround**:
```csharp
using OrgEntity = AmiaReforged.PwEngine.Features.WorldEngine.Organizations.Organization;
```

**Better Solution**:
- [ ] Determine if Database.Entities.Organization is legacy/unused
- [ ] If so, consider renaming or removing it
- [ ] If both needed, ensure clear naming convention

---

## 📝 Missing Implementations

### 5. In-Memory Repository Implementations (for testing)
**What's Needed**:
```csharp
// Tests/Systems/WorldEngine/Organizations/TestOrganizationRepository.cs
internal class TestOrganizationRepository : IOrganizationRepository
{
    private readonly List<IOrganization> _organizations = [];

    public void Add(IOrganization organization) { ... }
    public IOrganization? GetById(OrganizationId id) { ... }
    // ... etc
}

// Tests/Systems/WorldEngine/Organizations/TestOrganizationMemberRepository.cs
internal class TestOrganizationMemberRepository : IOrganizationMemberRepository
{
    private readonly List<OrganizationMember> _members = [];

    public void Add(OrganizationMember member) { ... }
    public OrganizationMember? GetByCharacterAndOrganization(...) { ... }
    // ... etc
}
```

**Action Items**:
- [ ] Create TestOrganizationRepository
- [ ] Create TestOrganizationMemberRepository
- [ ] Update existing tests to use them

### 6. BDD Tests for Commands
**Missing Test Files**:
```
Tests/Systems/WorldEngine/Organizations/
  - OrganizationCommandTests.cs (Create, AddMember, RemoveMember, AssignRole)
  - OrganizationQueryTests.cs (GetDetails, GetMembers, GetCharacterOrganizations)
  - DiplomaticRelationCommandTests.cs (if we implement diplomatic commands)
```

**Test Coverage Needed**:
- [ ] CreateOrganization - success, validation failures, parent not found
- [ ] AddMember - success, already member, banned member
- [ ] RemoveMember - success, permissions check, can't remove higher rank
- [ ] AssignRole - success, permissions, already has role
- [ ] RevokeRole - success, permissions, doesn't have role
- [ ] GetOrganizationDetails - found, not found
- [ ] GetOrganizationMembers - with/without filters
- [ ] GetCharacterOrganizations - active only

---

## 🏗️ Architectural Improvements

### 7. Persistent Repository Implementations
**What's Needed**: Database-backed repositories

**Files to Create**:
```
Features/WorldEngine/Organizations/
  - PersistentOrganizationRepository.cs
  - PersistentOrganizationMemberRepository.cs
  - OrganizationMemberMapper.cs (for EF mapping)
```

**Considerations**:
- Use existing PwContextFactory pattern
- Follow examples from Codex/Economy persistent repos
- Ensure OrganizationPersona is created/saved atomically with Organization

**Action Items**:
- [ ] Create database entities/migrations if needed
- [ ] Implement PersistentOrganizationRepository
- [ ] Implement PersistentOrganizationMemberRepository
- [ ] Wire up to dependency injection

### 8. Additional Commands (Nice to Have)
**Not Yet Implemented**:
```csharp
// DissolveOrganizationCommand - shut down organization
// TransferLeadershipCommand - change leader
// UpdateOrganizationCommand - edit name/description
// PromoteMemberCommand - increase rank
// DemoteMemberCommand - decrease rank
// EstablishDiplomaticRelationCommand - set stance between orgs
```

**Priority**: Medium (core membership works without these)

**Action Items**:
- [ ] Implement if time permits
- [ ] Add to backlog for future work

### 9. Additional Queries (Nice to Have)
**Not Yet Implemented**:
```csharp
// GetOrganizationsByTypeQuery - filter by Guild/Government/etc
// GetOrganizationHierarchyQuery - get parent + all children
// GetMembersWithRoleQuery - who has "Treasurer" role?
// GetDiplomaticRelationsQuery - all relations for an org
```

**Priority**: Medium

**Action Items**:
- [ ] Implement as needed
- [ ] Can be added incrementally

---

## ✅ What's Complete

### Value Objects ✅
- [x] `OrganizationRank` enum (Pending → Recruit → Member → Officer → Leader)
- [x] `MemberRole` value object (Leader, Treasurer, Recruiter, etc.)
- [x] `DiplomaticStance` enum (Allied → Neutral → Hostile → War)
- [x] `MembershipStatus` enum (Pending, Active, Inactive, Departed, Expelled, Banned)

### Domain Models ✅
- [x] `OrganizationMember` entity
  - Has rank, roles, status, joined date, departed date
  - Helper methods: HasRole(), CanManageMembers(), IsLeader()
- [x] `DiplomaticRelation` entity
  - Source/target orgs, stance, treaties
  - Helper methods: IsPositive(), IsNegative(), AtWar()

### Commands ✅ (Defined, not tested)
- [x] `CreateOrganizationCommand` + Handler
- [x] `AddMemberCommand` + Handler
- [x] `RemoveMemberCommand` + Handler
- [x] `AssignRoleCommand` + Handler
- [x] `RevokeRoleCommand` + Handler

### Queries ✅ (Defined, not tested)
- [x] `GetOrganizationDetailsQuery` + Handler
- [x] `GetOrganizationMembersQuery` + Handler
- [x] `GetCharacterOrganizationsQuery` + Handler

### Tests ✅ (Basic domain tests only)
- [x] `OrganizationMembershipTests.cs` - 11 tests for OrganizationMember entity
- [x] `DiplomaticRelationTests.cs` - 9 tests for DiplomaticRelation entity

---

## 🎯 Immediate Next Steps (Priority Order)

### Step 1: Fix Compilation (CRITICAL)
1. Create `IOrganizationRepository.cs` in proper location
2. Create `IOrganizationMemberRepository.cs` in proper location
3. Remove duplicate interface definitions from command files
4. Add missing using statements
5. Resolve Organization namespace ambiguity

### Step 2: Build Verification
1. Run `dotnet build AmiaReforged.PwEngine`
2. Fix any remaining compilation errors
3. Ensure zero errors before proceeding

### Step 3: Create Test Repositories
1. Create `TestOrganizationRepository`
2. Create `TestOrganizationMemberRepository`
3. Verify they implement interfaces correctly

### Step 4: Write BDD Command Tests
1. Create `OrganizationCommandTests.cs`
2. Test CreateOrganization (happy path + failures)
3. Test AddMember (happy path + edge cases)
4. Test RemoveMember (permissions, rank checks)
5. Test AssignRole/RevokeRole

### Step 5: Write BDD Query Tests
1. Create `OrganizationQueryTests.cs`
2. Test GetOrganizationDetails
3. Test GetOrganizationMembers
4. Test GetCharacterOrganizations

### Step 6: Run All Tests
1. `dotnet test --filter "Organization"`
2. Fix failures
3. Achieve 100% pass rate

### Step 7: Documentation
1. Update `PHASE3_4_ORGANIZATIONS.md` with completion status
2. Create `PHASE3_4_ORGANIZATIONS_COMPLETE.md` summary
3. Update `REFACTORING_INDEX.md`

---

## 🐛 Known Issues

### Issue 1: Organization vs IOrganization
**Problem**: Existing code uses `IOrganization` interface but also has `Organization` class

**Impact**: CreateOrganizationCommand returns `IOrganization` from factory method

**Resolution Needed**:
- Ensure repository accepts `IOrganization` (it does)
- Ensure all code uses interface, not concrete class
- Check if persistence layer can handle interface

### Issue 2: PersonaId Integration
**Problem**: Organizations must create OrganizationPersona when created

**Current State**: CreateOrganizationHandler attempts this but IPersonaRepository may not be accessible

**Resolution Needed**:
- Verify IPersonaRepository exists and is injectable
- Ensure atomic save of Organization + OrganizationPersona
- Test that `OrganizationId.ToPersonaId()` works correctly

### Issue 3: Founder Membership
**Problem**: CreateOrganizationCommand has `FounderId` but doesn't create membership

**Current Approach**: Command creates org, separate AddMemberCommand adds founder

**Questions**:
- Is this the right pattern? (Probably yes - keeps commands atomic)
- Should we have a `FoundOrganizationCommand` that does both?
- Or just document that client must call AddMember after Create?

---

## 📊 Completion Estimate

| Task | Estimated Time | Priority |
|------|----------------|----------|
| Fix compilation errors | 30 min | CRITICAL |
| Create test repositories | 20 min | HIGH |
| Write command tests | 1 hour | HIGH |
| Write query tests | 45 min | HIGH |
| Run & fix all tests | 30 min | HIGH |
| Persistent repositories | 2 hours | MEDIUM |
| Additional commands | 1 hour | LOW |
| Documentation | 30 min | MEDIUM |

**Total Core Work**: ~3.5 hours
**Total With Extras**: ~6 hours

---

## 🚀 Success Criteria

Organizations Phase 3.4 is complete when:

- [ ] All code compiles with zero errors
- [ ] Repository interfaces extracted to proper files
- [ ] Test repositories implemented
- [ ] BDD tests written and passing for all commands
- [ ] BDD tests written and passing for all queries
- [ ] CreateOrganization creates both Organization and OrganizationPersona
- [ ] Membership management (add/remove/roles) works
- [ ] Permission checks work (can't remove higher rank)
- [ ] Status tracking works (Active, Banned, Expelled, etc.)
- [ ] Documentation updated

**Bonus** (not required for completion):
- [ ] Persistent repositories implemented
- [ ] Additional commands (Dissolve, Transfer Leadership, etc.)
- [ ] Diplomatic relations fully implemented
- [ ] Integration with Industries (Guilds govern industries)

---

## 📁 File Checklist

### Files to Create:
- [ ] `/Features/WorldEngine/Organizations/IOrganizationRepository.cs`
- [ ] `/Features/WorldEngine/Organizations/IOrganizationMemberRepository.cs`
- [ ] `/Features/WorldEngine/Organizations/PersistentOrganizationRepository.cs` (later)
- [ ] `/Features/WorldEngine/Organizations/PersistentOrganizationMemberRepository.cs` (later)
- [ ] `/Tests/Systems/WorldEngine/Organizations/TestOrganizationRepository.cs`
- [ ] `/Tests/Systems/WorldEngine/Organizations/TestOrganizationMemberRepository.cs`
- [ ] `/Tests/Systems/WorldEngine/Organizations/OrganizationCommandTests.cs`
- [ ] `/Tests/Systems/WorldEngine/Organizations/OrganizationQueryTests.cs`
- [ ] `/Features/WorldEngine/PHASE3_4_ORGANIZATIONS_COMPLETE.md`

### Files to Update:
- [ ] Remove interface from `/Application/Organizations/Commands/CreateOrganizationCommand.cs`
- [ ] Remove interface from `/Application/Organizations/Commands/AddMemberCommand.cs`
- [ ] Add using statements to all command/query files
- [ ] Update `REFACTORING_INDEX.md` when complete

---

## 💡 Notes for Next Developer

### Key Decisions Made:
1. **OrganizationId Deduplication**: We use SharedKernel version ONLY (see ORGANIZATIONID_DEDUPLICATION_FIX.md)
2. **Persona Integration**: Organizations ARE Personas, must create both atomically
3. **Command Atomicity**: CreateOrganization doesn't add founder - use AddMemberCommand separately
4. **Permission Model**: Officers can manage members, but can't affect equal/higher rank

### Patterns to Follow:
- Look at Industries CQRS implementation (just completed)
- Look at Codex/Economy for command/query patterns
- Use BDD test style (Given/When/Then comments)
- Repository interfaces in domain folder, implementations separate
- Test repositories in test folder

### Common Pitfalls:
- Don't forget CancellationToken parameter on handlers
- Use `CommandResult.Ok()` and `.Fail()`, not Success/Failure
- Return `Task.FromResult()` not async if no await
- OrganizationMember uses SharedKernel value objects for Rank/Status/Role

---

**Created**: October 28, 2025
**Last Updated**: October 28, 2025
**Status**: Ready for pickup in new session

Good luck! 🚀

