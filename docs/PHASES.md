# CrossDrive Phases

## Phase 0 — Foundation and documentation

Status: **complete**

Established the Unity scaffold, project folders, version-control exclusions, MVP constraints, and initial module boundaries.

## Phase 1 — Local mechanic prototype

Status: **complete**

Provides one procedural arena, four local keyboard schemes, a complete randomized cross-control cycle, private assignment, anonymous/reveal/known play, movement/boost, gems, pits/non-eliminating respawn, three scoring strategies, development telemetry, and EditMode domain tests.

The 4/40/4/35/8-second timings, movement tuning, colors, layout, gem placement, and respawn delay remain playtest parameters.

## Phase 2 — Four-player multiplayer foundation

Status: **in progress — transport blocked on manual Photon setup**

Implemented in the repository:

- transport-separated room/player/match domain;
- exactly four active player slots, ready state, host-only start, and fifth-player rejection;
- host-generated Phase 1 control assignment and safe disconnect handling;
- private pre-reveal assignment view and authoritative reveal publication;
- authoritative timestamp phases (`LOBBY`, `ASSIGNMENT`, `COUNTDOWN`, `ANONYMOUS`, `REVEAL`, `KNOWN`, `RESULTS`);
- Siphon-default authoritative scoring with exactly-once gem/pit event guards;
- replicated-state DTOs for scores, six gems, four cars, respawns, phase, and reveal mapping;
- sequenced steer/boost input routed to the controlled car;
- minimal mobile/editor multiplayer UI shell and a separate Phase 2 scene; and
- EditMode tests for multiplayer domain invariants.

Still requiring manual/external setup:

- Photon Fusion 2 SDK import and Fusion AppId;
- a concrete Fusion Host Mode implementation of `IRoomTransport` plus network runner/behaviour prefabs;
- multi-peer or four-client transport verification;
- Android Build Support modules, package identifier, and a real Development Build.

Phase 2 intentionally excludes RevenueCat, accounts/auth, cloud persistence, public matchmaking, friends, cosmetics, progression, weapons, multiple arenas, production art, voice chat, analytics services, advanced anti-cheat, reconnect, and in-match host migration.

## Later phases

Final scoring, objective/economy, revenge mechanics, production content, and monetization remain unresolved and require explicit approval before implementation.
