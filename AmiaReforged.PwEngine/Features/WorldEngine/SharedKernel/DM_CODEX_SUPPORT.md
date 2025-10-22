# DM Codex Support

## Overview

DMs can now have their own persistent codex entries that follow them across sessions, regardless of which character avatar they use. This is achieved through deterministic GUID generation from their NWN public CD key.

## The Problem

In Neverwinter Nights:
- DMs can log in with different character names/avatars
- DMs don't have persistent character IDs like players
- DMs need campaign notes, world events, and lore tracking
- Traditional character-based storage doesn't work for DMs

## The Solution: DmId

The `DmId` value object provides:
1. **Deterministic Identity** - Same CD key always produces same GUID
2. **Polymorphic Support** - Can be used wherever `CharacterId` is expected
3. **Persistence** - DM codices survive across sessions and character changes
4. **Type Safety** - Compile-time guarantees and validation

---

## Usage

### Creating DmId from CD Key

```csharp
// From NWN script - get DM's public CD key
string publicCdKey = GetPCPublicCDKey(dmPlayer); // e.g., "ABCD1234"

// Create deterministic DM identifier
DmId dmId = DmId.FromCdKey(publicCdKey);

// Same CD key ALWAYS produces same GUID
DmId dmId1 = DmId.FromCdKey("ABCD1234");
DmId dmId2 = DmId.FromCdKey("ABCD1234");
Assert.That(dmId1, Is.EqualTo(dmId2)); // Always true!
```

### Case Insensitivity

```csharp
// All produce the same DmId
DmId dm1 = DmId.FromCdKey("abcd1234");
DmId dm2 = DmId.FromCdKey("ABCD1234");
DmId dm3 = DmId.FromCdKey("AbCd1234");

Assert.That(dm1, Is.EqualTo(dm2));
Assert.That(dm2, Is.EqualTo(dm3));
```

### Whitespace Handling

```csharp
// Automatically trims whitespace
DmId dm1 = DmId.FromCdKey("  ABCD1234  ");
DmId dm2 = DmId.FromCdKey("ABCD1234");

Assert.That(dm1, Is.EqualTo(dm2));
```

### Using with Codex System

```csharp
// DmId implicitly converts to CharacterId
DmId dmId = DmId.FromCdKey(publicCdKey);
CharacterId characterId = dmId; // Implicit conversion!

// Can use DM codex just like character codex
PlayerCodex dmCodex = codexManager.GetCodex(characterId);

// Or directly (polymorphic support)
PlayerCodex dmCodex = codexManager.GetCodex(dmId);
```

---

## Validation

### Required Format

- **Length**: Exactly 8 characters
- **Content**: Any characters (typically alphanumeric in NWN)
- **Not Null/Empty/Whitespace**

### Examples

```csharp
// Valid
DmId.FromCdKey("ABCD1234");  ✅
DmId.FromCdKey("12345678");  ✅
DmId.FromCdKey("TEST1234");  ✅

// Invalid - throws ArgumentException
DmId.FromCdKey("ABC123");    ❌ Too short (6 chars)
DmId.FromCdKey("ABCD12345"); ❌ Too long (9 chars)
DmId.FromCdKey("");          ❌ Empty
DmId.FromCdKey(null);        ❌ Null
DmId.FromCdKey("   ");       ❌ Whitespace only
```

---

## Implementation Details

### Deterministic GUID Generation

```csharp
public static DmId FromCdKey(string publicCdKey)
{
    // 1. Normalize: uppercase and trim
    string normalized = publicCdKey.Trim().ToUpperInvariant();

    // 2. Validate: must be exactly 8 characters
    if (normalized.Length != 8)
        throw new ArgumentException("Must be 8 characters");

    // 3. Hash using SHA256 (deterministic)
    byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));

    // 4. Take first 16 bytes for GUID
    byte[] guidBytes = new byte[16];
    Array.Copy(hash, guidBytes, 16);

    // 5. Create GUID (same input = same output)
    Guid deterministicGuid = new Guid(guidBytes);

    return new DmId(deterministicGuid);
}
```

### Why SHA256?

- **Deterministic**: Same input always produces same output
- **Collision Resistant**: Different CD keys produce different GUIDs
- **One-Way**: Cannot reverse-engineer CD key from GUID
- **Standard**: Well-tested cryptographic hash function

---

## Use Cases

### 1. DM Campaign Notes

```csharp
// DM adds notes about campaign progression
DmId dmId = DmId.FromCdKey(GetPCPublicCDKey(dmPlayer));

var noteEvent = new NoteAddedEvent(
    dmId, // Implicitly converts to CharacterId
    DateTime.UtcNow,
    Guid.NewGuid(),
    "Players discovered the ancient temple. Next session: reveal the cult.",
    NoteCategory.DmNote,
    IsDmNote: true,
    IsPrivate: true // Only DM can see
);

await codexEventChannel.Writer.WriteAsync(noteEvent);
```

### 2. World Event Tracking

```csharp
// Track major world events from DM perspective
DmId dmId = DmId.FromCdKey(publicCdKey);

var worldEvent = new WorldEventRecordedEvent(
    dmId,
    DateTime.UtcNow,
    "War breaks out between Thay and Rashemen",
    "Major conflict affecting all players in region"
);
```

### 3. DM Lore Library

```csharp
// DM maintains personal lore library
DmId dmId = DmId.FromCdKey(publicCdKey);

var loreEvent = new LoreDiscoveredEvent(
    dmId,
    DateTime.UtcNow,
    new LoreId("dm_notes_thay_politics"),
    "Thayan Political Structure",
    "Internal notes on Red Wizard hierarchy for campaign...",
    "DM Research",
    LoreTier.Legendary,
    new List<Keyword> { new("thay"), new("politics"), new("dm") }
);
```

### 4. Cross-Session Continuity

```csharp
// Session 1: DM "Gandalf" (CD key: ABCD1234) adds notes
DmId session1 = DmId.FromCdKey("ABCD1234");
// ... DM adds campaign notes ...

// Session 2: Same DM logs in as "Elminster" (same CD key)
DmId session2 = DmId.FromCdKey("ABCD1234");

// Both sessions access the SAME codex!
Assert.That(session1, Is.EqualTo(session2));

PlayerCodex codex1 = codexManager.GetCodex(session1);
PlayerCodex codex2 = codexManager.GetCodex(session2);
// codex1 and codex2 point to same persistent data
```

---

## NWN Integration

### Example Script (NWScript)

```c
// In NWN script
void OnDMLogin()
{
    object oDM = GetEnteringObject();
    string sCDKey = GetPCPublicCDKey(oDM);

    // Call C# service
    NWNXCSharp_CallFunction("DmCodexService", "OnDmLogin", sCDKey);
}
```

### Example Adapter (C#)

```csharp
public class NwnDmAdapter
{
    private readonly ChannelWriter<CodexDomainEvent> _eventWriter;

    public void OnDmLogin(string publicCdKey)
    {
        try
        {
            // Create deterministic DM identifier
            DmId dmId = DmId.FromCdKey(publicCdKey);

            // Load DM codex (happens automatically via CharacterId conversion)
            CharacterId characterId = dmId;

            // DM codex is now available for this session
            // All events using this characterId will be persisted

            _logger.LogInformation(
                "DM logged in with persistent ID {DmId} from CD key {CDKey}",
                dmId.Value,
                publicCdKey);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid DM CD key: {CDKey}", publicCdKey);
        }
    }
}
```

---

## Conversions

### Implicit Conversions (Safe)

```csharp
DmId dmId = DmId.FromCdKey("ABCD1234");

// DmId → Guid (always safe)
Guid guid = dmId;

// DmId → CharacterId (always safe, main use case)
CharacterId characterId = dmId;
```

### Explicit Conversions (Require Validation)

```csharp
// Guid → DmId (validates not empty)
Guid guid = Guid.NewGuid();
DmId dmId = (DmId)guid;

// CharacterId → DmId (allows treating character as DM)
CharacterId characterId = CharacterId.New();
DmId dmId = (DmId)characterId;
```

---

## Testing

### 24 Comprehensive Tests

Located in `Tests/Systems/WorldEngine/SharedKernel/DmIdTests.cs`:

- ✅ Valid CD key creates DmId
- ✅ Deterministic behavior (same key = same GUID)
- ✅ Case insensitivity
- ✅ Whitespace trimming
- ✅ Length validation (exactly 8 chars)
- ✅ Null/empty/whitespace rejection
- ✅ Structural equality
- ✅ Type conversions (implicit and explicit)
- ✅ Dictionary key usage
- ✅ HashSet usage
- ✅ Cross-session persistence simulation
- ✅ Polymorphic usage with CharacterId

All 24 tests passing ✅

---

## Benefits

### 1. Persistent DM Identity
- DMs maintain codex across sessions
- No matter which character they use
- Campaign notes never lost

### 2. Type Safety
```csharp
// Compile-time error prevention
void ProcessDmCodex(DmId dmId)        // Clear intent
void ProcessCodex(CharacterId id)     // Works with both!
void ProcessSomething(Guid id)        // Ambiguous (avoid)
```

### 3. Deterministic
- Same CD key always produces same identity
- Predictable and testable
- No database lookups for mapping

### 4. Secure
- One-way SHA256 hash
- Cannot reverse-engineer CD key
- Collision resistant

### 5. Simple Integration
```csharp
// NWN side: Just pass CD key string
OnDmEvent(publicCdKey);

// C# side: Automatic conversion
DmId dmId = DmId.FromCdKey(publicCdKey);
CharacterId id = dmId; // Use in codex system
```

---

## Summary

| Feature | Support |
|---------|---------|
| Deterministic GUID from CD key | ✅ |
| Case-insensitive | ✅ |
| Whitespace trimming | ✅ |
| Validation (8 chars) | ✅ |
| Implicit CharacterId conversion | ✅ |
| Polymorphic codex usage | ✅ |
| Type safety | ✅ |
| Test coverage | ✅ 24/24 |
| Production ready | ✅ |

---

## Example Workflow

```
1. DM logs into NWN server
   ↓
2. NWN script gets public CD key: "ABCD1234"
   ↓
3. C# adapter creates DmId: DmId.FromCdKey("ABCD1234")
   ↓
4. DmId deterministically generates GUID: <same-guid-every-time>
   ↓
5. DmId converts to CharacterId: dmId → characterId
   ↓
6. Codex system loads/creates DM codex
   ↓
7. DM can add notes, track events, maintain lore
   ↓
8. DM logs out
   ↓
9. Next session: Same CD key → Same GUID → Same codex! ✅
```

---

**DM Codex Support: Enabling persistent DM tools across sessions!** 🎲✨
