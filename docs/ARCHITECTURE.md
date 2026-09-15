# CrossDrive Architecture

## Architecture rule

Phase 2 adds networking around the Phase 1 domain. `ControlAssignmentService`, `CarMovement`, scoring strategies, arena rules, and typed scoring events remain transport-independent. A network adapter is responsible for moving commands and snapshots; it must not become a second scoring or assignment implementation.

## Layers

| Layer | Types | Responsibility |
| --- | --- | --- |
| Phase 1 gameplay | `ControlAssignmentService`, `CarMovement`, `ArenaManager`, scoring strategies | Existing reusable mechanics and rules |
| Room domain | `RoomService`, `NetworkPlayer` | Four slots, room code, identity, ready/host gates, disconnect state |
| Match domain | `NetworkSessionCoordinator`, `NetworkRoundCoordinator` | Host-only assignment, network-time phases, authoritative events/scores, interruption |
| Privacy | `PrivateAssignmentState`, `PrivateAssignmentView` | Per-recipient pre-reveal facts and delayed public mapping |
| Input bridge | `NetworkInputCommand`, `NetworkInputBuffer`, `NetworkCarSimulationAdapter` | Small sequenced client command and assigned-car routing |
| Replication contract | `NetworkMatchSnapshot`, `NetworkGemState`, `NetworkCarState`, `NetworkClientView` | Copy-only state a concrete transport serializes to clients |
| Transport boundary | `IRoomTransport` | Create/join/ready/start/input/rematch/leave operations |
| Runtime shell | `NetworkBootstrap` | Minimal HOME/JOIN/LOBBY/GAME/RESULTS UI and mobile/editor input |

`FusionRoomTransport` implements the lobby milestone behind `IRoomTransport`; `NetworkBootstrap` creates it in the existing Phase 2 scene. See [Milestone 1](PHASE2_MILESTONE1.md) for the implemented create-only Realtime handoff and reliable lobby protocol. The round, assignment and scoring sections below describe the existing domain foundation and future transport work.

## Authority model

Photon Fusion should use Host Mode. The host/state authority owns:

- room membership, slot assignment, readiness, and match start;
- generation of the one complete four-player control cycle;
- assignment distribution and the reveal switch;
- phase and end-time advancement;
- movement simulation for all cars;
- gem generation/visibility, collision acceptance, and respawn time;
- pit acceptance, recent-contact attribution, scoring, and car respawn state; and
- final scores and rematch state.

Clients send only sequenced steer/boost commands and UI requests. Every mutating `NetworkRoundCoordinator` method requires an authority flag; a Fusion adapter must pass `Object.HasStateAuthority`/runner authority rather than client-provided data.

The driver sends input, but state authority applies it to `ResolveControlledCar(sessionPlayerId)`. Network object authority must never be assigned to the car owner merely because that player owns the car in the game fiction.

## Assignment privacy

The full `ControlAssignment` exists only on state authority during `ASSIGNMENT`, `COUNTDOWN`, and `ANONYMOUS`. For each client the host creates one `PrivateAssignmentView` containing:

- that player's own car ID/color;
- the controlled car ID/color;
- the controlled car owner's session ID/display name; and
- the literal hidden own-driver label `???`.

The view has no own-driver field and no full mapping. The concrete transport must deliver it only to its intended `PlayerRef`. At the authoritative transition to `REVEAL`, `PublishReveal` copies the unchanged mapping into public snapshot state. The same copy remains through `KNOWN` and `RESULTS`.

## Round synchronization

The network sequence is:

`LOBBY → ASSIGNMENT → COUNTDOWN (4s) → ANONYMOUS (40s) → REVEAL (4s) → KNOWN (35s) → RESULTS (8s minimum)`

`ASSIGNMENT` lasts until state authority confirms all targeted private views were sent. All timed phases use one authoritative absolute network timestamp. Clients render remaining time from the replicated phase/end time and never advance phases locally. Results stay visible for rematch/leave after the eight-second minimum.

## Gems, pits, respawn, and scores

Each gem has `gemId + generation`. State authority accepts a pickup only when that generation is visible and unprocessed, applies exactly one neutral `GemCollectedEvent`, hides the gem, and schedules generation increment/visibility after two seconds.

Each car has `carId + lifeId`. State authority accepts a pit only when that life is active and unprocessed, applies exactly one `CarEnteredPitEvent`, marks the car respawning, and releases a new life after three seconds. The pit event includes the host-observed last-contact car and age, so existing Split Purse attribution remains reusable.

`ScoringCoordinator` remains the single score writer. Multiplayer defaults to Siphon; development adapters may choose Commission or Split Purse before a match. Snapshots copy scores and object state so client code cannot mutate authority-owned collections.

## Disconnect behavior

In the lobby, a disconnected client is removed and their slot reopens without moving other seats. The creator remains the fixed host; host departure ends the room and never promotes a client. Start is disabled until exactly four connected players are ready.

During a match, the player is marked disconnected and the round enters `Interrupted` with a clear reason. No replacement, reconnect, or in-match host migration is attempted because changing a cycle participant would invalidate private information and scoring attribution.

## Photon boundary and limitations

Fusion 2.1.2 is installed. The concrete lobby adapter uses one runner per attempt, authoritative room rules, and reliable lobby messages. No gameplay network objects/prefabs or assignment/score replication are implemented by Milestone 1. Existing Photon settings supply the App ID.

No RevenueCat, accounts, cloud database, public matchmaking, progression, weapons, multiple arenas, voice, production art, analytics backend, or advanced anti-cheat are part of Phase 2.
