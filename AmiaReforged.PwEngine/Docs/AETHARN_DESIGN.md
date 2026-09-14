# Aetharn - Dice Game Design Document

## Implementation Status

| Component | Status | Notes |
|-----------|--------|-------|
| `AetharnConstants.cs` | ✅ Done | All magic literals centralized |
| `ScoringResult.cs` | ✅ Done | Immutable result type |
| `AetharnScorer.cs` | ✅ Done | Pure scoring logic, 56 passing tests |
| `AetharnScorerTests.cs` | ✅ Done | Full coverage of all scoring combinations |
| `TurnState.cs` | ✅ Done | Immutable turn state with Die record, HoldResult, RollResult, HotDiceResult |
| `TurnStateTests.cs` | ✅ Done | 46 passing tests covering all turn operations |
| Event Args | ✅ Done | 11 event args classes in Events/ subfolder |
| `AetharnPlayer.cs` | ⬜ Not Started | Player state (NWN boundary via ds_pckey) |
| `AetharnGame.cs` | ⬜ Not Started | Main game loop with events |
| `TurnTimer.cs` | ⬜ Not Started | 30s turn countdown |
| `AbsenceTracker.cs` | ⬜ Not Started | 60s grace period tracking |
| `AetharnService.cs` | ⬜ Not Started | Game instance management |
| `AetharnTableDiscovery.cs` | ⬜ Not Started | Trigger/chair discovery |
| NUI Components | ⬜ Not Started | Controller, View, Bindings |

**Current Phase:** Phase 2a Complete (Turn State + Events)

---

## Overview

Aetharn is a push-your-luck dice game for 2-6 players. Each game table represents an independent game instance, managed by a central service responsible for spawning and tracking active games.

---

## Game Rules

### Setup
- **Players:** 2–6
- **Dice:** 6d6
- **Objective:** First player to reach **5000 points** wins
- **Starting Score:** 0 points for all players
- Player order is **randomly determined** at game start and persists for the entire session

### Turn Flow
1. Roll all six dice
2. After each roll:
   - Set aside **scoring dice** (1s, 5s, or triples+)
   - You **must** set aside at least one scoring die to continue
   - Optionally reroll remaining non-scoring dice
3. **Hot Dice:** If all six dice score, you may roll all six again
4. **Bust:** If no dice score on a roll, your turn ends with **0 points** for that round
5. **Bank:** You may stop at any point and bank your accumulated points

### Scoring Table

| Combination | Points |
|-------------|--------|
| Single 1 | 100 |
| Single 5 | 50 |
| Three of a kind (2-6) | Face value × 100 |
| Three 1s | 1000 |
| Four of a kind | 2× three-of-a-kind value |
| Five of a kind | 4× three-of-a-kind value |
| Six of a kind | 8× three-of-a-kind value |
| Straight (1-2-3-4-5-6) | 1500 |
| Three pairs | 1500 |

---

## Architecture

### Design Principles
- **Event-driven:** All state changes emit C# events for UI updates
- **Instance-based:** Each table spawns its own `AetharnGame` instance
- **Decoupled:** Game logic is separate from NUI rendering
- **Observable:** UI subscribes to game events, never polls

### Constants

```csharp
public static class AetharnConstants
{
    // Placeable Tags
    public const string TriggerTag = "game_aetharn";
    public const string ChairTag = "aetharn_player_seat";
    public const string ObserverTag = "game_aetharn_observe";
    
    // Game Configuration
    public const int WinningScore = 5000;
    public const int MaxPlayers = 6;
    public const int MinPlayers = 2;
    public const int DiceCount = 6;
    
    // Timers (seconds)
    public const int TurnTimeLimitSeconds = 30;
    public const int AbsenceGracePeriodSeconds = 60;
    
    // Grace Period Warning Intervals (seconds remaining)
    public static readonly int[] GraceWarningIntervals = { 45, 30, 15, 10, 5 };
    
    // Scoring
    public const int SingleOnePoints = 100;
    public const int SingleFivePoints = 50;
    public const int ThreeOnesPoints = 1000;
    public const int StraightPoints = 1500;
    public const int ThreePairsPoints = 1500;
}
```

---

## Core Components

### 1. `AetharnPlayer`
Represents a player in the game.

```csharp
public class AetharnPlayer
{
    public NwPlayer NwPlayer { get; }
    public string Name { get; }
    public int Score { get; private set; }
    public bool HasFolded { get; private set; }
    public PlayerPresence Presence { get; private set; }
    public DateTime? AbsentSince { get; private set; }  // When they left the chair
    public NwPlaceable? AssignedChair { get; set; }     // The chair they're sitting in
    
    public void AddScore(int points);
    public void Fold();
    public void MarkAbsent();
    public void MarkPresent();
}

public enum PlayerPresence
{
    Present,    // Sitting in chair
    Absent,     // Left chair, grace period active
    Gone        // Grace period expired, removed from game
}
```

### 2. `DiceRoll`
Immutable representation of a dice roll.

```csharp
public readonly record struct DiceRoll(int[] Values)
{
    public int Count => Values.Length;
}
```

### 3. `ScoringResult`
Result of evaluating a set of dice.

```csharp
public class ScoringResult
{
    public int Points { get; }
    public int[] ScoringDice { get; }      // Indices of dice that scored
    public int[] NonScoringDice { get; }   // Indices of dice that didn't score
    public ScoringType Type { get; }       // Enum describing the scoring combination
    public bool IsBust => Points == 0 && ScoringDice.Length == 0;
    public bool IsHotDice => NonScoringDice.Length == 0;
}

public enum ScoringType
{
    None,
    Singles,
    ThreeOfAKind,
    FourOfAKind,
    FiveOfAKind,
    SixOfAKind,
    Straight,
    ThreePairs
}
```

### 4. `AetharnScorer`
Static scoring logic, pure functions.

```csharp
public static class AetharnScorer
{
    public static ScoringResult Evaluate(int[] dice);
    public static int CalculateThreeOfAKindValue(int faceValue);
    public static bool IsStraight(int[] dice);
    public static bool IsThreePairs(int[] dice);
}
```

### 5. `TurnState`
Tracks the current player's turn progress.

```csharp
public class TurnState
{
    public AetharnPlayer Player { get; }
    public int AccumulatedPoints { get; private set; }
    public int[] CurrentDice { get; private set; }
    public int[] HeldDice { get; private set; }
    public int RemainingDice => 6 - HeldDice.Length;
    public bool HasUsedHotDice { get; private set; }  // Only one Hot Dice per turn!
    
    public void HoldDice(int[] indices);
    public void AddPoints(int points);
    public void UseHotDice();  // Sets HasUsedHotDice = true
    public void Reset();       // Resets HasUsedHotDice for new turn
}
```

### 6. `AetharnGame`
The main game instance. Manages state and emits events.

```csharp
public class AetharnGame
{
    // State
    public Guid GameId { get; }
    public NwPlaceable Table { get; }                    // The table placeable
    public IReadOnlyList<AetharnPlayer> Players { get; }
    public IReadOnlyList<NwPlayer> Spectators { get; }   // Observers watching
    public AetharnPlayer? CurrentPlayer { get; }
    public GamePhase Phase { get; }
    public TurnState? CurrentTurn { get; }
    public AetharnPlayer? Winner { get; }
    public GameEndReason? EndReason { get; }
    public DateTime? TurnStartTime { get; }
    public TimeSpan TurnTimeRemaining { get; }
    
    // Configuration (from AetharnConstants)
    public int WinningScore { get; } = AetharnConstants.WinningScore;
    public TimeSpan TurnTimeLimit { get; } = TimeSpan.FromSeconds(AetharnConstants.TurnTimeLimitSeconds);
    public TimeSpan AbsenceGracePeriod { get; } = TimeSpan.FromSeconds(AetharnConstants.AbsenceGracePeriodSeconds);
    
    // Events
    public event EventHandler<GameStartedEventArgs>? GameStarted;
    public event EventHandler<TurnStartedEventArgs>? TurnStarted;
    public event EventHandler<DiceRolledEventArgs>? DiceRolled;
    public event EventHandler<DiceHeldEventArgs>? DiceHeld;
    public event EventHandler<PointsScoredEventArgs>? PointsScored;
    public event EventHandler<PlayerBustedEventArgs>? PlayerBusted;
    public event EventHandler<TurnEndedEventArgs>? TurnEnded;
    public event EventHandler<TurnTimedOutEventArgs>? TurnTimedOut;      // NEW
    public event EventHandler<PlayerFoldedEventArgs>? PlayerFolded;
    public event EventHandler<PlayerAbsentEventArgs>? PlayerAbsent;      // NEW
    public event EventHandler<PlayerReturnedEventArgs>? PlayerReturned;  // NEW
    public event EventHandler<PlayerRemovedEventArgs>? PlayerRemoved;    // NEW (grace period expired)
    public event EventHandler<GameEndedEventArgs>? GameEnded;
    public event EventHandler<GameAbandonedEventArgs>? GameAbandoned;    // NEW
    public event EventHandler<HotDiceEventArgs>? HotDice;
    public event EventHandler<SpectatorJoinedEventArgs>? SpectatorJoined;   // NEW
    public event EventHandler<SpectatorLeftEventArgs>? SpectatorLeft;       // NEW
    public event EventHandler<LobbyPlayerJoinedEventArgs>? LobbyPlayerJoined;   // NEW
    public event EventHandler<LobbyPlayerLeftEventArgs>? LobbyPlayerLeft;       // NEW
    public event EventHandler<LobbyClearedEventArgs>? LobbyCleared;             // NEW
    
    // Actions
    public void Start();                           // Transitions from Lobby to InProgress
    public void Roll();
    public void HoldDice(int[] diceIndices);
    public void Bank();
    public void Fold(AetharnPlayer player);
    public void PlayerSatDown(NwPlayer player, NwPlaceable chair);
    public void PlayerStoodUp(NwPlayer player);
    public void AddSpectator(NwPlayer player);
    public void RemoveSpectator(NwPlayer player);
}

public enum GamePhase
{
    Lobby,              // Waiting for players to sit, no time limit
    InProgress,         // Game is active
    Ended,              // Game ended with a winner
    Abandoned           // All players left, no winner
}

public enum GameEndReason
{
    WinnerReached5000,
    LastPlayerStanding,
    AllPlayersLeft       // Abandoned
}
```

### 7. `AetharnService`
Singleton service managing all active game instances.

```csharp
public class AetharnService
{
    private readonly Dictionary<Guid, AetharnGame> _activeGames = new();
    private readonly Dictionary<NwPlaceable, Guid> _tableToGame = new();
    private readonly Dictionary<NwPlaceable, NwPlaceable> _chairToTable = new();
    private readonly Dictionary<NwPlaceable, NwPlaceable> _observerToTable = new();
    
    // Events
    public event EventHandler<GameCreatedEventArgs>? GameCreated;
    public event EventHandler<GameDestroyedEventArgs>? GameDestroyed;
    
    // Methods
    public AetharnGame GetOrCreateLobby(NwPlaceable table);
    public AetharnGame? GetGame(Guid gameId);
    public AetharnGame? GetGameForTable(NwPlaceable table);
    public AetharnGame? GetGameForChair(NwPlaceable chair);
    public void DestroyGame(Guid gameId);
    public IReadOnlyCollection<AetharnGame> GetActiveGames();
    
    // Table/Chair Discovery (called on module load)
    public void RegisterTable(NwPlaceable table, IEnumerable<NwPlaceable> chairs);
    public void RegisterObserverPlaceable(NwPlaceable observer, NwPlaceable table);
    
    // Chair event handlers
    public void OnChairUsed(NwPlaceable chair, NwPlayer player);
    public void OnPlayerLeftChair(NwPlaceable chair, NwPlayer player);
    
    // Observer placeable handlers
    public void OnObserverUsed(NwPlaceable observer, NwPlayer player);
}
```

### 8. `AetharnTableDiscovery`
Finds and registers all Aetharn tables on module load.

```csharp
public class AetharnTableDiscovery
{
    // Finds all triggers tagged "game_aetharn"
    // For each trigger, finds chairs inside
    // Registers chairs -> nearest table relationship
    // Finds "game_aetharn_observe" placeables and links to nearest table
    
    public void DiscoverAndRegister(AetharnService service);
}
```

### 9. `TurnTimer`
Manages the 30-second turn countdown.

```csharp
public class TurnTimer : IDisposable
{
    public TimeSpan TimeLimit { get; }
    public TimeSpan Remaining { get; }
    public bool IsRunning { get; }
    
    public event EventHandler? TimerExpired;
    public event EventHandler<TimeSpan>? TimerTick;  // Optional: for UI updates
    
    public void Start();
    public void Stop();
    public void Reset();
}
```

### 10. `AbsenceTracker`
Tracks absent players and their grace periods.

```csharp
public class AbsenceTracker : IDisposable
{
    public TimeSpan GracePeriod { get; }
    
    // Tracks: PlayerId -> AbsentSince
    private readonly Dictionary<Guid, DateTime> _absentPlayers;
    
    public event EventHandler<AetharnPlayer>? GracePeriodExpired;
    public event EventHandler<GraceWarningEventArgs>? GraceWarning;  // Periodic announcements
    
    public void MarkAbsent(AetharnPlayer player);
    public void MarkPresent(AetharnPlayer player);  // Returns true if within grace period
    public bool IsAbsent(AetharnPlayer player);
    public TimeSpan GetRemainingGrace(AetharnPlayer player);
}

public class GraceWarningEventArgs : EventArgs
{
    public AetharnPlayer Player { get; init; }
    public TimeSpan TimeRemaining { get; init; }  // Announced at 45s, 30s, 15s, 10s, 5s
}
```

---

## Table & Chair Integration

### Placeable Setup in Toolset

```
Trigger: "game_aetharn" (AetharnConstants.TriggerTag)
├── Contains: 2-6 Chair placeables tagged "aetharn_player_seat" (AetharnConstants.ChairTag)
├── Contains: 1 Table placeable (visual only, any tag)
└── Nearby: "game_aetharn_observe" placeable (AetharnConstants.ObserverTag) for spectators
```

**Important:** Chairs MUST be tagged with `aetharn_player_seat` to be recognized as game seats. This avoids false positives from decorative placeables inside the trigger area.

### How It Works

1. **On Module Load:**
   - `AetharnTableDiscovery` finds all triggers tagged `game_aetharn`
   - For each trigger, finds all chairs inside the trigger bounds
   - Finds any `game_aetharn_observe` placeables and links to nearest trigger/table
   - Registers chair → table mappings with `AetharnService`

2. **Chair OnUsed:**
   - Player clicks chair → `AetharnService.OnChairUsed(chair, player)`
   - If no lobby exists for this table → Create new lobby (GamePhase.Lobby)
   - If lobby/game exists → Add player to lobby OR rejoin game (if within grace period)
   - Player is seated (animation/position handled separately)
   - NUI opens for player

3. **Player Leaves Chair:**
   - Detected via heartbeat check or explicit stand-up action
   - If in Lobby → Remove from lobby, if last player → clear lobby
   - If InProgress → Mark player Absent, start 60s grace period

4. **Observer OnUsed:**
   - Player clicks observer placeable → `AetharnService.OnObserverUsed(observer, player)`
   - Adds player as spectator to the linked game
   - Opens read-only NUI (no action buttons)

### Chair Detection Pseudocode

```csharp
public void DiscoverAndRegister(AetharnService service)
{
    foreach (var trigger in NwTrigger.FindObjectsWithTag(AetharnConstants.TriggerTag))
    {
        var chairs = new List<NwPlaceable>();
        
        // Find all placeables inside trigger with the chair tag
        foreach (var placeable in trigger.GetObjectsInArea<NwPlaceable>())
        {
            if (placeable.Tag == AetharnConstants.ChairTag)
            {
                chairs.Add(placeable);
            }
        }
        
        if (chairs.Count < AetharnConstants.MinPlayers)
        {
            Log.Warning($"Aetharn trigger at {trigger.Position} has only {chairs.Count} chairs (min: {AetharnConstants.MinPlayers})");
            continue;
        }
        
        if (chairs.Count > AetharnConstants.MaxPlayers)
        {
            Log.Warning($"Aetharn trigger at {trigger.Position} has {chairs.Count} chairs (max: {AetharnConstants.MaxPlayers}), extras will be ignored");
            chairs = chairs.Take(AetharnConstants.MaxPlayers).ToList();
        }
        
        // Find the table (could be tagged or just the center placeable)
        var table = FindTableInTrigger(trigger);
        
        service.RegisterTable(table, chairs);
        
        // Hook up chair events
        foreach (var chair in chairs)
        {
            chair.OnUsed += (usedBy) => service.OnChairUsed(chair, usedBy);
        }
    }
    
    // Find observer placeables and link to nearest table
    foreach (var observer in NwPlaceable.FindObjectsWithTag(AetharnConstants.ObserverTag))
    {
        var nearestTable = FindNearestRegisteredTable(observer);
        if (nearestTable == null)
        {
            Log.Warning($"Observer placeable at {observer.Position} has no nearby Aetharn table");
            continue;
        }
        
        service.RegisterObserverPlaceable(observer, nearestTable);
        
        observer.OnUsed += (usedBy) => service.OnObserverUsed(observer, usedBy);
    }
}
```

---

## Event Args

```csharp
public class GameStartedEventArgs : EventArgs
{
    public IReadOnlyList<AetharnPlayer> Players { get; init; }
    public AetharnPlayer FirstPlayer { get; init; }
}

public class TurnStartedEventArgs : EventArgs
{
    public AetharnPlayer Player { get; init; }
    public int TurnNumber { get; init; }
}

public class DiceRolledEventArgs : EventArgs
{
    public int[] DiceValues { get; init; }
    public ScoringResult ScoringResult { get; init; }
}

public class DiceHeldEventArgs : EventArgs
{
    public int[] HeldDiceValues { get; init; }
    public int PointsFromHeld { get; init; }
    public int RemainingDiceCount { get; init; }
}

public class PointsScoredEventArgs : EventArgs
{
    public AetharnPlayer Player { get; init; }
    public int PointsThisTurn { get; init; }
    public int TotalScore { get; init; }
}

public class PlayerBustedEventArgs : EventArgs
{
    public AetharnPlayer Player { get; init; }
    public int LostPoints { get; init; }
}

public class TurnEndedEventArgs : EventArgs
{
    public AetharnPlayer Player { get; init; }
    public AetharnPlayer NextPlayer { get; init; }
    public bool WasBust { get; init; }
    public int PointsBanked { get; init; }
}

public class PlayerFoldedEventArgs : EventArgs
{
    public AetharnPlayer Player { get; init; }
    public int RemainingPlayerCount { get; init; }
}

public class GameEndedEventArgs : EventArgs
{
    public AetharnPlayer Winner { get; init; }
    public IReadOnlyList<AetharnPlayer> FinalStandings { get; init; }
}

public class HotDiceEventArgs : EventArgs
{
    public AetharnPlayer Player { get; init; }
    public int AccumulatedPoints { get; init; }
}

public class TurnTimedOutEventArgs : EventArgs
{
    public AetharnPlayer Player { get; init; }
    public int LostPoints { get; init; }  // Points accumulated this turn, now lost
}

public class PlayerAbsentEventArgs : EventArgs
{
    public AetharnPlayer Player { get; init; }
    public TimeSpan GracePeriod { get; init; }  // How long they have to return
}

public class PlayerReturnedEventArgs : EventArgs
{
    public AetharnPlayer Player { get; init; }
    public TimeSpan TimeRemaining { get; init; }  // How much grace period was left
}

public class PlayerRemovedEventArgs : EventArgs
{
    public AetharnPlayer Player { get; init; }
    public string Reason { get; init; }  // "Grace period expired" / "Disconnected"
    public int RemainingPlayerCount { get; init; }
}

public class GameAbandonedEventArgs : EventArgs
{
    public IReadOnlyList<AetharnPlayer> Players { get; init; }  // Final state
}

public class SpectatorJoinedEventArgs : EventArgs
{
    public NwPlayer Spectator { get; init; }
    public int TotalSpectators { get; init; }
}

public class SpectatorLeftEventArgs : EventArgs
{
    public NwPlayer Spectator { get; init; }
    public int TotalSpectators { get; init; }
}

public class LobbyPlayerJoinedEventArgs : EventArgs
{
    public AetharnPlayer Player { get; init; }
    public int TotalPlayers { get; init; }
    public bool CanStart { get; init; }  // True if 2+ players
}

public class LobbyPlayerLeftEventArgs : EventArgs
{
    public AetharnPlayer Player { get; init; }
    public int TotalPlayers { get; init; }
}

public class LobbyClearedEventArgs : EventArgs
{
    public string Reason { get; init; }  // "All players left"
}
```

---

## NUI Integration

### `AetharnNuiController`
Handles NUI window lifecycle and user input.

```csharp
public class AetharnNuiController : IGameObserver
{
    private readonly AetharnGame _game;
    private readonly NwPlayer _player;
    private NuiWindowToken? _windowToken;
    
    // Binds to AetharnGame events
    // Translates game state to NUI bindings
    // Handles NUI button clicks -> game actions
}
```

### NUI Layout Concept

```
┌─────────────────────────────────────────────────────────┐
│  AETHARN - Table #1                              [X]    │
├─────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────┐    │
│  │  SCOREBOARD                                      │    │
│  │  Player 1: 2350 pts  ◄── Current                │    │
│  │  Player 2: 1800 pts                             │    │
│  │  Player 3: 3100 pts                             │    │
│  │  Player 4: (Folded)                             │    │
│  └─────────────────────────────────────────────────┘    │
│                                                         │
│  ┌─────────────────────────────────────────────────┐    │
│  │  CURRENT TURN: Player 1                         │    │
│  │  Turn Points: 450                               │    │
│  │                                                 │    │
│  │  Dice:  [1] [5] [3] [3] [3] [2]                │    │
│  │         [✓] [✓] [✓] [✓] [✓] [ ]                │    │
│  │                                                 │    │
│  │  Held:  [1] [5]  = 150 pts                     │    │
│  └─────────────────────────────────────────────────┘    │
│                                                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐              │
│  │   ROLL   │  │   BANK   │  │   FOLD   │              │
│  └──────────┘  └──────────┘  └──────────┘              │
│                                                         │
│  Status: Select dice to hold, then Roll or Bank        │
└─────────────────────────────────────────────────────────┘
```

---

## State Machine

### Game Lifecycle

```
    ┌─────────────────────────────────────────────────────────────┐
    │                         LOBBY                               │
    │  - Player sits in chair → LobbyPlayerJoined                 │
    │  - Player leaves chair → LobbyPlayerLeft                    │
    │  - No time limit                                            │
    │  - If all players leave → LobbyClearedEvent, destroy lobby  │
    └─────────────────────────┬───────────────────────────────────┘
                              │ Start() [2+ players]
                              ▼
    ┌─────────────────────────────────────────────────────────────┐
    │                      IN PROGRESS                            │
    │                                                             │
    │   ┌──────────────────────────────────────────────────────┐  │
    │   │ TURN LOOP (30s timer per turn)                       │  │
    │   │                                                      │  │
    │   │  TurnStarted → Roll → [HoldDice → Roll]* → Bank     │  │
    │   │       │                    │                 │       │  │
    │   │       │ Bust               │ HotDice         │       │  │
    │   │       ▼                    ▼                 ▼       │  │
    │   │  TurnEnded(0)         Roll(6)           TurnEnded    │  │
    │   │       │                                      │       │  │
    │   │       └──────────────────────────────────────┘       │  │
    │   │                          │                           │  │
    │   │  TurnTimedOut ──────────►│ (auto-bust)              │  │
    │   └──────────────────────────┼───────────────────────────┘  │
    │                              │                              │
    │   Player leaves chair:                                      │
    │     → PlayerAbsent (60s grace period starts)                │
    │     → If returns: PlayerReturned                            │
    │     → If timer expires: PlayerRemoved (folded)              │
    │                                                             │
    │   If player count drops to 1:                               │
    │     → Last player wins by default                           │
    │                                                             │
    │   If player count drops to 0:                               │
    │     → GameAbandoned                                         │
    └─────────────────────────┬───────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              ▼               ▼               ▼
    ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
    │    ENDED     │  │    ENDED     │  │  ABANDONED   │
    │ (Winner:5000)│  │ (LastPlayer) │  │ (No Winner)  │
    └──────────────┘  └──────────────┘  └──────────────┘
```

### Player Presence State Machine

```
    ┌───────────┐  leaves chair   ┌───────────┐  60s expires   ┌───────────┐
    │  PRESENT  │ ───────────────►│  ABSENT   │ ──────────────►│   GONE    │
    │ (in chair)│                 │ (grace)   │                │ (removed) │
    └───────────┘                 └─────┬─────┘                └───────────┘
         ▲                              │
         │         returns to chair     │
         └──────────────────────────────┘
```

---

## Turn Flow Detail

```
1. TurnStarted event fired
   - 30 second timer starts
   - Timer displayed in NUI

2. Player rolls all available dice (6 initially)

3. DiceRolled event fired with ScoringResult
   
   IF Bust:
     - PlayerBusted event fired
     - TurnEnded event fired (points = 0)
     - Next player's turn
   
   ELSE:
     - Player selects dice to hold (must hold at least 1 scoring die)
     - DiceHeld event fired
     
     IF HotDice (all 6 scored) AND hasNotUsedHotDiceThisTurn:
       - HotDice event fired
       - Timer RESETS to 30 seconds
       - Mark hasUsedHotDiceThisTurn = true
       - Player may roll all 6 again
     
     IF HotDice BUT already used Hot Dice this turn:
       - No bonus roll allowed (prevents runaway wins)
       - Player must Bank or continue with remaining dice
     
     Player chooses:
       A) Roll remaining dice → Go to step 3
       B) Bank → PointsScored event, TurnEnded event, next player

   IF Timer expires (30s):
     - TurnTimedOut event fired
     - Treat as Bust (lose all accumulated points this turn)
     - TurnEnded event fired (points = 0)
     - Next player's turn
```

---

## Directory Structure

```
MiniGame/
├── Aetharn/
│   ├── AETHARN_DESIGN.md           # This document
│   ├── AetharnConstants.cs         # All magic literals centralized
│   ├── AetharnPlayer.cs
│   ├── AetharnGame.cs
│   ├── AetharnService.cs
│   ├── AetharnScorer.cs
│   ├── AetharnTableDiscovery.cs    # Finds triggers/chairs on load
│   ├── TurnState.cs
│   ├── TurnTimer.cs                # 30s turn countdown
│   ├── AbsenceTracker.cs           # 60s grace period tracking
│   ├── ScoringResult.cs
│   ├── Events/
│   │   ├── GameStartedEventArgs.cs
│   │   ├── TurnStartedEventArgs.cs
│   │   ├── TurnTimedOutEventArgs.cs
│   │   ├── DiceRolledEventArgs.cs
│   │   ├── DiceHeldEventArgs.cs
│   │   ├── PointsScoredEventArgs.cs
│   │   ├── PlayerBustedEventArgs.cs
│   │   ├── TurnEndedEventArgs.cs
│   │   ├── PlayerFoldedEventArgs.cs
│   │   ├── PlayerAbsentEventArgs.cs
│   │   ├── PlayerReturnedEventArgs.cs
│   │   ├── PlayerRemovedEventArgs.cs
│   │   ├── GameEndedEventArgs.cs
│   │   ├── GameAbandonedEventArgs.cs
│   │   ├── HotDiceEventArgs.cs
│   │   ├── SpectatorJoinedEventArgs.cs
│   │   ├── SpectatorLeftEventArgs.cs
│   │   ├── LobbyPlayerJoinedEventArgs.cs
│   │   ├── LobbyPlayerLeftEventArgs.cs
│   │   └── LobbyClearedEventArgs.cs
│   └── Nui/
│       ├── AetharnNuiController.cs
│       ├── AetharnNuiView.cs
│       ├── AetharnNuiBindings.cs
│       └── AetharnSpectatorView.cs         # Read-only view for observers
└── LiarsDice/
    └── ... (existing)
```

---

## Implementation Order

1. **Phase 1: Core Logic**
   - [ ] `AetharnScorer` - Pure scoring functions with unit tests
   - [ ] `ScoringResult` - Immutable result type
   - [ ] `AetharnPlayer` - Player state

2. **Phase 2: Game State**
   - [ ] `TurnState` - Turn tracking
   - [ ] `AetharnGame` - Main game loop with events
   - [ ] Event args classes

3. **Phase 3: Service Layer**
   - [ ] `AetharnService` - Game instance management

4. **Phase 4: NUI**
   - [ ] `AetharnNuiView` - Layout definition
   - [ ] `AetharnNuiBindings` - Reactive bindings
   - [ ] `AetharnNuiController` - Input handling

5. **Phase 5: Integration**
   - [ ] Hook into table placeables
   - [ ] Player join/leave handling
   - [ ] Persistence (optional)

---

## Edge Cases to Handle

### Player Presence
1. **Player stands up from chair mid-game** → Mark absent, start 60s grace period
2. **Player returns within grace period** → Restore to Present, continue normally
3. **Grace period expires** → Remove player (auto-fold), PlayerRemoved event
4. **Player disconnects** → Same as standing up (grace period applies)
5. **Player is absent when their turn comes** → Skip their turn, continue to next

### Game State
6. **All but one player removed** → Last player wins automatically (LastPlayerStanding)
7. **All players removed** → Game abandoned (no winner)
8. **Player tries to act when absent** → Reject action
9. **Turn timer expires** → Auto-bust, TurnTimedOut event

### Lobby State
10. **Last player in lobby leaves** → Clear lobby, destroy game instance
11. **Player tries to start with < 2 players** → Reject action

### General
12. **Simultaneous winners** → First to reach 5000 wins (turn order matters)
13. **Player tries to roll with no dice held** → Reject action
14. **Player tries to hold non-scoring dice** → Reject action
15. **Game table placeable destroyed while game active** → End game, notify players

---

## Testing Strategy

### TDD Approach & NWN Boundary Constraints

**Critical Constraint:** NWN VM code cannot be mocked or called from unit tests. All game logic must be isolated from NWN types at testable boundaries.

#### Testable (Pure C#, No NWN Dependencies)
- `AetharnConstants` - Static values only
- `AetharnScorer` - Pure functions: `int[] → ScoringResult`
- `ScoringResult` - Immutable data class
- `TurnState` - State tracking (uses primitives, not `NwPlayer`)
- Game state transitions in `AetharnGame` (via abstracted player IDs)

#### Boundary/Glue Code (Not Unit Testable)
- `AetharnPlayer` - Wraps `NwPlayer`, tested via integration
- `AetharnService` - Manages `NwPlaceable` references
- `AetharnTableDiscovery` - Queries NWN objects
- NUI Controllers - Direct NWN API calls

#### Test Structure
```
MiniGame/
├── Aetharn/
│   ├── Tests/
│   │   ├── AetharnScorerTests.cs      # Scoring combinations
│   │   ├── TurnStateTests.cs          # Turn state transitions
│   │   └── GameStateTests.cs          # Game phase transitions (mocked players)
│   └── ... (production code)
```

#### Test Framework
- **NUnit** with **FluentAssertions**
- Tests live in same project: `AmiaReforged.PwEngine`
- Pattern: Arrange-Act-Assert with descriptive test names

### Unit Tests
- `AetharnScorer` - All 9+ scoring combinations with edge cases
- `TurnState` - Dice holding, point accumulation, Hot Dice flag
- Game state machine - Phase transitions, player management (using test doubles)

### Integration Tests (Manual/Server)
- Chair/table discovery on module load
- NUI event flow
- Player presence detection

---

## Design Decisions

| Question | Decision |
|----------|----------|
| Minimum score to "get on the board"? | No - any points can be banked |
| Support spectators? | **Yes** - via `game_aetharn_observe` placeables |
| Persist game state across server restarts? | No - games are ephemeral |
| Turn timer? | **Yes - 30 seconds per turn** |
| Player disconnect handling? | **60 second grace period to return** |
| Lobby timeout? | No - lobbies persist until all players leave |
| Hot Dice timer behavior? | **Reset timer, but only ONE Hot Dice allowed per turn** (prevents runaway wins) |
| Spectator view? | **Real-time** - spectators see the same dice as players |
| Grace period announcements? | **Yes** - announce remaining time periodically |

---

## Open Questions

*All questions resolved - see Design Decisions table above.*
