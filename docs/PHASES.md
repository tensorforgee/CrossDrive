# Crosswire Phases

## Phase 0 — Foundation and documentation

Status: **complete**

Phase 0 established the Unity 6.3 LTS scaffold, project-owned folder structure, version-control exclusions, locked MVP constraints, and initial module boundaries.

## Phase 1 — Local mechanic prototype

Status: **current**

Phase 1 provides:

- one procedural top-down test arena with three pits and six respawning gems;
- four distinct placeholder cars and four local keyboard schemes;
- one complete randomized cross-control cycle;
- private pre-round assignment, anonymous play, halftime reveal, known-driver play, and round end;
- auto-acceleration, steering, boost, momentum, and non-eliminating respawn;
- three experimental scoring implementations behind one replaceable scoring interface;
- development-only JSON event and score telemetry; and
- edit-mode tests for assignment, scoring, attribution, switching, and round transitions.

The current 4/40/4/35/8-second phase timings, movement tuning, colors, layout, gem placement, and respawn delay are playtest values rather than locked final design.

Phase 1 explicitly excludes networking, RevenueCat, final scoring, progression, accounts, matchmaking, multiple maps, final art, and unrequested gameplay systems.

## Later phases

Later phases remain planning placeholders. Before implementing them, resolve or deliberately defer:

- **UNRESOLVED:** final scoring model;
- **UNRESOLVED:** final objective/economy;
- **UNRESOLVED:** revenge mechanic;
- **UNRESOLVED:** final round timing; and
- **UNRESOLVED:** premium content details.

Technology choices and acceptance criteria for later phases will be documented when those phases are approved.
