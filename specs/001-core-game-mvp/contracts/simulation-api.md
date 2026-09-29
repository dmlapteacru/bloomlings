# Contract: Deterministic Simulation Core (`Bloomlings.Core`)

This is the boundary between the rules and everything else: the Unity presentation, the solver, the generator, the
pipeline and the tests. The sources are research R3, R4 and R10, the spec FR-003 to FR-050, and `CLAUDE.md` invariants.

## Guarantees

1. **Determinism.** `Load(definition, picture)` followed by the same list of accepted commands always produces the same
   events, the same final state and the same `StateHash`. This holds across platforms (Mono/IL2CPP/.NET), animation
   speed and timing (FR-024, SC-005).
2. **Settled states.** Every accepted command returns only after the automatic work reaches its fixpoint (R3). No
   state exists "between" rounds from the caller's point of view.
3. **Purity.** The core has no dependency on `UnityEngine`, wall-clock time, threads, floating point, `System.Random`
   or hash-order iteration. Budgets are counted in nodes.
4. **Exact matching.** Allocation is keyed by `VariantId`, never by family (FR-003).

## Surface (C#, netstandard2.1)

These are signatures only; they are illustrative and not implementation.

```csharp
public sealed class LevelSession
{
    public static LevelSession Load(LevelDefinition definition, BasePicture picture, SessionOptions options);

    public LevelView View { get; }               // read-only snapshot for rendering and queries
    public LevelStatus Status { get; }           // Playing | Won | Jammed | Stuck
    public ulong StateHash { get; }              // Zobrist hash of the logical state

    public CommandResult Apply(Command command); // Accepted (with events) or Rejected (with reason); never throws for game rules
    public CommandCheck Check(Command command);  // same validation as Apply, without mutation (for button states)
    public IReadOnlyList<Command> CommandLog { get; }
}

public abstract record Command;
public sealed record TapPod(string PodId) : Command;
public sealed record UseExtraSlot() : Command;
public sealed record UseShuffle() : Command;
public sealed record UseReturn(int SlotIndex) : Command;
public sealed record UseBloomBurst(VariantId Variant) : Command;
public sealed record Restart() : Command;

public sealed record CommandResult(bool Accepted, RejectReason? Reason, IReadOnlyList<GameEvent> Events);
```

`SessionOptions` carries `ContentVersion` (used to salt the PRNG) and `ShuffleNodeBudget`, which is part of the
content/config contract so that every device uses the same value.

## Rejection reasons (no state change)

| Reason | When |
|---|---|
| `NotExposed` | The pod is buried |
| `Locked` | The pod's key has not been collected (FR-034) |
| `NoFreeSlot` | No free usable slot (FR-014) |
| `NotEnoughSlotsForGroup` | A connected group needs more free slots than there are (FR-035) |
| `LevelNotPlaying` | The level is Won, Jammed or Stuck, and the command is not a recovery or Restart |
| `BoosterNotApplicable` | FR-051. Examples: Return with no unfinished waiting pod; Extra Slot already used; Shuffle with fewer than 2 eligible pods; Bloom Burst on a variant that is not visible |

Charges, costs and ads are **not** checked by the core. The client's economy layer decides whether a booster command
may be issued. The core only decides whether the command is legal.

## Events

Events are ordered. Each carries `Round`, where 0 means the command's own immediate effects.

| Event | Payload | Presentation use |
|---|---|---|
| `PodCommitted` | podId, slotIndex, stack, fromDepth | pod flies to its slot |
| `MysteryPodRevealed` | podId, variant | flip animation |
| `TileCleared` | cell, variant, podId, round, routeFromEntry[] | Bloomling walks the route and restores the tile |
| `LayerRevealed` | cell, newTopVariant | next layer appears |
| `CellOpened` | cell | finished picture shows through (FR-007) |
| `MysteryTileRevealed` | cell, variant | flip |
| `KeyCollected` | keyId, cell | key flies to its lock |
| `LockOpened` | target (pod, slot or special) | unlock animation |
| `SpecialProgressed` / `SpecialTriggered` | specialId, progress/total, effect cells | counter, then transformation |
| `PodCompleted` | podId, slotIndex | pod leaves |
| `SlotFreed` | slotIndex | slot empties |
| `ExtraSlotAdded` | slotIndex = 5 | sixth slot appears |
| `TrayShuffled` | new stacks | tray rearrange animation |
| `PodReturned` | podId, stack | pod flies back to the top of its stack |
| `VariantBurst` | variant, cells[], podIds[] | Bloom Burst effect |
| `LevelWon` | boostersUsed | finished picture, reward flow |
| `LevelJammed` / `LevelStuck` | eligibleRecoveries[] | Jam screen, board stays visible (FR-027) |

## Rules pinned by this contract

- **Reachability**: BFS from entries over open cells with 4-neighbourhood. Route distance is the number of BFS steps
  to the adjacent open cell, plus 1 (FR-010).
- **Candidate order**: (route distance ↑, row ↓ from the bottom, column ↑) (FR-021).
- **Allocation per round**: pods are served in slot-age order; each claims `min(remaining, unclaimed candidates)`
  (FR-017, FR-020).
- **End-of-fixpoint check**: Win, then Jam, then Stuck (FR-025, FR-026).
- **Extra Slot**: at most once per level (FR-043).
- **Return**: the pod goes to the top of its original stack with its remaining count (FR-045).
- **Bloom Burst**: removes every layer of the variant (visible and hidden) and every pod of the variant (FR-050).
- **Shuffle**: follows the constructive algorithm in R10. The PRNG is SplitMix64 → xoshiro256\*\*, seeded with
  `hash(seed, contentVersion, shuffleUses, StateHash)`. The node budget comes from `SessionOptions`.

## Solver-facing extensions (`Bloomlings.Solver`)

```csharp
public interface ISolver
{
    SolveResult Solve(LevelSession start, SolveOptions options);                  // winning trace or unsolvable/unknown
    SolveResult FindJam(LevelSession start, SolveOptions options);                // jam witness (FR-081)
    FairnessResult CheckPlayerInformation(LevelSession start, SolveOptions o);    // mystery fairness (R8)
    Metrics Measure(LevelSession start, SolveOptions options);                    // difficulty metrics (FR-082)
}
```

The solver clones sessions and applies commands through the same `Apply`. It has no separate rules implementation.
