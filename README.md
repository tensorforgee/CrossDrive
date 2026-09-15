# CrossDrive

CrossDrive is a top-down 2D multiplayer party game. Each player owns one car while controlling a different player's car in a single four-player cycle.

The repository contains two runnable entry scenes:

- `Assets/_Project/Scenes/Phase1Prototype.unity` — the working four-player local hot-seat prototype.
- `Assets/_Project/Scenes/Phase2Multiplayer.unity` — the Phase 2 Fusion Host Mode lobby.

Unity version: **6000.3.23f1**.

## Phase 2 status

Fusion **2.1.2** is imported. Milestone 1 implements room creation, joining, four stable seats, ready/unready, fixed-host start gating and room rejection through the existing `IRoomTransport`. The Phase2Multiplayer scene starts this adapter automatically. Start ends at an explicit milestone screen; gameplay networking remains future work.

See [Milestone 1 architecture, exact manual steps and verification](docs/PHASE2_MILESTONE1.md). The transport-neutral assignment/round/scoring foundation remains present, but is not networked in this milestone.

## Android Development Build prerequisites

Current project settings already target ARM64, Android minimum API 25, and mark the application as a game. Touch steer/boost controls are present in the Phase 2 shell.

1. Unity's Android platform player is present, but the bundled `SDK`, `NDK`, and `OpenJDK` module directories are absent. In Unity Hub, modify Unity `6000.3.23f1` and add **Android SDK & NDK Tools** and **OpenJDK** (and verify **Android Build Support** remains selected).
2. In Unity, switch **File > Build Profiles** to Android and select `Phase2Multiplayer` as the startup scene.
3. Set a project-owned Android package identifier in Player Settings; the repository intentionally does not invent an organization/domain identifier.
4. Keep ARM64 enabled. Select **Development Build** (and Script Debugging only when needed).
5. After Fusion setup, confirm `PhotonAppSettings`, internet permission, region settings, and all network prefabs/config assets are included.
6. Build and install the same APK on four devices, then test create/join/ready/start, input ownership, reveal, scoring, disconnect, rematch, and leave.

The Android toolchain is incomplete and no Android build has been run.

## Phase 1 local prototype

Open `Phase1Prototype.unity` and press Play. Four local players use these controls:

| Player | Own car | Steer | Boost |
| --- | --- | --- | --- |
| Player 1 | Red | `A` / `D` | `W` |
| Player 2 | Cyan | arrows | `Up Arrow` |
| Player 3 | Yellow | `J` / `L` | `I` |
| Player 4 | Magenta | `C` / `V` | `F` |

Phase 1 retains its 4/40/4/35/8-second timing, six gems, two-second gem respawn, three-second non-eliminating car respawn, telemetry, and development scoring selection (`1` Commission, `2` Siphon, `3` Split Purse).

## Tests

Open **Window > General > Test Runner**, select **EditMode**, and run all tests. Phase 2 tests cover host assignment, private/revealed mapping, mapping preservation, exactly-once gems and pits, authoritative timestamps, four-player start gating, fifth-player rejection, disconnect handling, score snapshot consistency, and input routing.

## Known limitations

- Gameplay replication and private assignment delivery are outside Milestone 1.
- See the Milestone 1 verification record for transport checks. Separate-device play, latency/prediction, and Android builds remain unverified.
- In-match disconnect ends the round; reconnect and host migration are intentionally not implemented.
- Room codes are private session names, not public matchmaking.
- The Phase 2 scene is a functional debug shell, not production art or UX.

## Documentation

- [Game specification](docs/GAME_SPEC.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Phases](docs/PHASES.md)
- [Phase 1 scoring design](docs/PHASE1_SCORING_DESIGN.md)
