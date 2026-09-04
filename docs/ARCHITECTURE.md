# Crosswire Architecture

## Status and intent

Phase 1 implements the smallest local prototype that can test the cross-control mechanic. Runtime visuals are procedural placeholders, gameplay stays independent of any final scoring model, and no online or product systems are present.

## Runtime ownership

| Type | Responsibility |
| --- | --- |
| `PlayerIdentity` | Stable player index, display name, owned-car color/name |
| `LocalKeyboardInput` | The four fixed local keyboard schemes and raw intent reads |
| `ControlAssignmentService` | Creates and validates one complete randomized player cycle |
| `CarController` | Binds a car owner to its current driver and forwards that driver's input |
| `CarMovement` | Auto-acceleration, steering, boost, speed cap, and lateral slide |
| `ArenaManager` | Single arena bounds, pit regions, and owner spawn points |
| `CarContactTracker` | Emits bump events and retains the latest car contact for shove attribution |
| `RespawnSystem` | Emits pit/respawn events and restores cars after three seconds without elimination |
| `GemSystem` | Maintains six gems and emits owner/driver-resolved collection events |
| `RoundStateMachine` | Pure, testable phase transitions and timers |
| `RoundManager` | Assignment briefing, phase orchestration, input gating, and round restart |
| `IScoringStrategy` | Neutral six-event scoring contract plus score/feedback reads |
| `ScoringCoordinator` | Restricts mode changes and derives per-event score deltas |
| `CommissionScoringStrategy` | Experimental owner-heavy cooperative scoring |
| `SiphonScoringStrategy` | Experimental driver-gain/owner-loss scoring |
| `SplitPurseScoringStrategy` | Experimental owner gem score and attributed shove score |
| `RoundTelemetryRecorder` | Development-only per-round JSON event ledger |
| `PrototypeHud` | Briefing, model/rule, scores, identity state, and reveal mapping |
| `CrossDriveGame` | Small composition root that builds the procedural prototype |

## Dependency flow

`CrossDriveGame` composes the feature services. Input intent is read by `CarController` and applied by `CarMovement`; it does not affect assignment or scoring rules. `RoundManager` controls whether cars and gems are active and owns round progression. Gems, contact tracking, and respawn emit neutral typed events to `ScoringCoordinator`, which delegates to the current `IScoringStrategy` and returns score deltas for telemetry.

Commission, Siphon, and Split Purse consume the same event surface. Keys `1`/`2`/`3` select a strategy only during pre-round or results, so a live round cannot change rules underneath players. Point values and phase timings are centralized in `Phase1PrototypeConfig`.

## Assignment and anonymity

The assignment is represented as `driver player -> owned car`. Generation shuffles all player IDs and links consecutive IDs into one closed cycle. Validation requires exactly one target per driver, exactly one driver per car, no self-target, no mutual pair, and a traversal that visits every player before returning to the start.

During pre-round, assignments are shown one player at a time behind a reveal/hide handoff. The anonymous HUD renders each player's owner/control references but displays `MY DRIVER: ???`. Halftime and later phases display the inverse mapping—each owned car and its driver. The mapping is not regenerated at halftime.

## Deliberately temporary values

The configured comparison protocol is a 4-second pre-round countdown, 40-second anonymous phase, 4-second reveal, 35-second known phase, and 8-second results phase. Gems respawn after two seconds and cars after three seconds. These remain experimental playtest values, not final design decisions.

## Excluded systems

Networking, matchmaking, accounts, persistence, progression, monetization/RevenueCat, multiple arenas, final art/audio, and final scoring are outside Phase 1.
