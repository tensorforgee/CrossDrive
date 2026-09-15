# CrossDrive Game Specification

## Status

This document records locked MVP rules and explicitly marked prototype parameters. Phase 2 does not resolve the final scoring/economy decisions.

## Game identity

- Title: **CrossDrive**
- Genre: top-down 2D multiplayer party game
- Phase 2 active player count: exactly 4
- Long-term MVP player count remains unresolved beyond the tested four-player mode
- MVP content: one arena
- Each player owns one toy/bumper car and controls a different player's car

## Control assignment

Assignments form one randomized cycle containing every active player:

- nobody controls their own car;
- each car has exactly one driver;
- each player controls exactly one car;
- no mutual two-player pair is allowed; and
- the cycle is unchanged across reveal and known play.

The Phase 2 host generates the cycle with the existing `ControlAssignmentService`. Before reveal, a client receives only their own car, the car they control, and that car's owner. Their own driver remains `???`. At reveal, the complete mapping is published to all clients at the same authoritative transition.

## Phase 2 room and round

The private room flow is HOME, room-code create/join, LOBBY, then host start. All four connected players must be ready. A fifth active player cannot join. There is no account, public matchmaking, or friends system.

The synchronized match sequence is:

| State | Rule |
| --- | --- |
| `LOBBY` | Membership/readiness; host may start only at 4/4 ready |
| `ASSIGNMENT` | Host creates the cycle and sends one private view per client |
| `COUNTDOWN` | 4 seconds |
| `ANONYMOUS` | 40 seconds; driver identities hidden |
| `REVEAL` | 4 seconds; full unchanged mapping becomes public |
| `KNOWN` | 35 seconds |
| `RESULTS` | Authoritative standings; 8-second minimum before normal rematch flow |

Clients do not advance state locally. Host/network timestamps are authoritative.

## Network gameplay rules

- Clients send only steer and boost intent.
- Host authority resolves the sender's assigned car, simulates all movement, and replicates car state.
- Six host-owned gems score exactly once per visible generation and respawn after two seconds.
- A car pit entry scores exactly once per car life and respawns after three seconds without elimination.
- Recent host-observed car contact is included for Split Purse shove attribution.
- Scores are written only by the host's existing `ScoringCoordinator` and replicated to every client.
- Multiplayer testing defaults to Siphon. Commission and Split Purse remain development-selectable before a match.

## Disconnects

Lobby disconnects remove the player and block start until the room returns to four ready clients. A match disconnect ends/interrupts the round safely instead of replacing the player and corrupting the cycle. Reconnect and advanced host migration are not Phase 2 requirements.

## Prototype status and unresolved decisions

The Phase 2 architecture is implemented independently of transport, but Photon Fusion is not installed and a real Fusion adapter/AppId still require manual setup. Real connectivity, multi-client play, prediction behavior, and Android builds are unverified.

Still unresolved:

- final scoring model and objective/economy;
- revenge mechanic;
- final round timing;
- long-term supported player-count range; and
- premium content details.

Phase 2 excludes RevenueCat, accounts/auth, cloud databases, public matchmaking, friends, cosmetics, progression, weapons, multiple arenas, production art, voice chat, analytics backends, and advanced anti-cheat.
