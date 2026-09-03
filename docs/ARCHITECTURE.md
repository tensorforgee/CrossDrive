# Crosswire Architecture

## Status and intent

Phase 0 reserves clear ownership boundaries without implementing game systems or selecting service providers. The structure is deliberately small so design decisions can evolve without early coupling.

## Module boundaries

| Module | Reserved responsibility | Phase 0 state |
| --- | --- | --- |
| `Core` | Shared game lifecycle concepts and feature-neutral contracts | Empty |
| `Gameplay` | Cars, arena rules, round flow, and control assignment behavior | Empty |
| `Input` | Player intent for left steering, right steering, and boost | Empty |
| `Scoring` | Scoring rules and score state, once the model is decided | Empty |
| `Networking` | Multiplayer transport, synchronization, sessions, and matchmaking boundaries | Empty |
| `UI` | Presentation and player-facing screens | Empty |
| `Monetization` | Premium-content and purchase boundaries | Empty |
| `Tests` | Edit-mode and play-mode tests as implementation begins | Empty |

Assets that belong to Crosswire live under `Assets/_Project`. Third-party content, if later approved, should remain outside this namespace so project-owned code and assets stay identifiable.

## Dependency direction

Feature modules should communicate through small contracts and game state owned at the appropriate boundary. In particular:

- gameplay rules must not depend directly on concrete input devices;
- scoring must remain separate from car control and round flow;
- networking must carry game intent/state without owning game rules;
- UI must observe or request behavior rather than become the source of game rules; and
- monetization must remain isolated from gameplay eligibility unless a later product decision explicitly requires otherwise.

Concrete interfaces, assemblies, state ownership, network authority, transport, input package, and persistence are intentionally not selected in Phase 0.

## Locked domain constraints

The architecture must be able to represent:

- 3–6 players, each owning one car;
- one randomized control-assignment cycle containing all players;
- no self-control or mutual two-player control pair;
- hidden driver identities in the first half and revealed identities at halftime;
- an unchanged control mapping in the second half, based on the current locked rule;
- auto-acceleration, left/right steering, and one boost action;
- no elimination; and
- one MVP arena.

These constraints describe required behavior only; they do not prescribe an implementation.

## Unresolved architecture inputs

The following are **UNRESOLVED** and must not be encoded as permanent architecture assumptions:

- final scoring model;
- final objective/economy;
- revenge mechanic;
- exact round timing; and
- premium content details.

Networking technology, matchmaking design, backend authority, input package, render pipeline, and RevenueCat integration details are also not selected or implemented in Phase 0.

