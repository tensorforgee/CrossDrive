# Crosswire

Crosswire is a top-down 2D multiplayer party game being built for the RevenueCat Shipaton 2026.

The project is currently in **Phase 1: local mechanic prototype**. It includes three explicitly experimental scoring strategies so the cross-control mechanic can be compared with four people sharing one keyboard. None is a final game rule.

## Unity setup

- Unity version: **6000.3.18f1 (Unity 6.3 LTS)**
- Rendering: built-in 2D
- Input: Unity's built-in keyboard input
- Optional packages: Unity Test Framework only

Open this repository folder directly from Unity Hub. Unity will generate local metadata and caches on first open; generated cache directories are excluded from version control.

## Run the prototype

1. Open `Assets/_Project/Scenes/Phase1Prototype.unity`.
2. Press Play.
3. During the private briefing, everyone except the named player looks away. The named player presses their boost key once to reveal their assignment, memorizes it, then presses it again to hide the card and pass the laptop.
4. In the editor or a Development Build, use `1`, `2`, or `3` during pre-round to select Commission, Siphon, or Split Purse. Scoring cannot be switched during active play.
5. After all four briefings, a 4-second countdown leads into the 40-second anonymous phase.
6. At the 4-second reveal, cars freeze and the complete driver mapping appears. The same mapping resumes for the 35-second known-driver phase.
7. Results remain for 8 seconds. Press `R` to restart immediately, or select the next scoring model with `1`/`2`/`3`.

The arena, cars, camera, and HUD are generated at runtime, so the checked-in scene intentionally contains only a marker object.

## Controls

| Player | Own car | Steer left/right | Boost |
| --- | --- | --- | --- |
| Player 1 | Red | `A` / `D` | `W` |
| Player 2 | Cyan | `Left Arrow` / `Right Arrow` | `Up Arrow` |
| Player 3 | Yellow | `J` / `L` | `I` |
| Player 4 | Magenta | `C` / `V` | `F` |

Cars auto-accelerate. Six gems respawn two seconds after collection. Driving off the platform or into any dark pit triggers a three-second respawn; players are never eliminated.

## Experimental scoring modes

| Key | Mode | Current test rule |
| --- | --- | --- |
| `1` | Commission | Gem: owner +3, driver +1. Pit: owner -2, driver -1. |
| `2` | Siphon | Gem: driver +2, owner -1. Pit: owner -1. |
| `3` | Split Purse | Gem: owner +3. Pit: owner -2; driver of a qualifying last-contact car +3. |

Split Purse uses a one-second contact-attribution window. Bumps never score directly.

Development/editor rounds write JSON telemetry to `Application.persistentDataPath/CrossDriveTelemetry`, including mapping, events, score deltas, and final scores.

## Tests

Open **Window > General > Test Runner**, select **EditMode**, and run all tests. Tests cover cycle invariants, all documented scoring deltas, attribution edge cases, model-switch restrictions, fixed mapping, and round timing.

## Phase 1 boundaries

Included: one arena, four local players, cross-control assignment, simple car motion, pits/respawn, gems, round phases, debug HUD, telemetry, and three pluggable experimental scoring strategies.

Not included: final scoring rules, networking, RevenueCat, progression, accounts, matchmaking, multiple maps, final art, or additional gameplay systems.

## Documentation

- [Game specification](docs/GAME_SPEC.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Phases](docs/PHASES.md)
