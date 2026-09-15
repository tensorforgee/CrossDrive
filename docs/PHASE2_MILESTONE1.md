# Phase 2 Milestone 1 — Fusion lobby

This milestone implements a real Fusion 2.1.2 Host Mode adapter for the existing Phase2Multiplayer scene. Start ends at a replicated `MatchStarted` marker. No assignments, cars, input, gems, pits, scores, reconnect, migration, or purchases are networked.

## Architecture and room flow

`NetworkBootstrap` creates `FusionRoomTransport`, which implements the existing `IRoomTransport`. It owns one fresh `NetworkRunner` per connection attempt and uses `NetworkSessionCoordinator.Room` for host-only lobby decisions. The existing round coordinator and Phase 1 systems remain intact and are not started by this adapter.

Create generates six characters from `ABCDEFGHJKMNPQRSTUVWXYZ23456789`. A Fusion-configured Realtime client calls `CreateAndJoinRoomAsync` (the installed implementation calls `OpCreateRoom`, not `OpJoinOrCreateRoom`). A collision fails and asks the user to create again. No room-list preflight or fallback join is used. The reserved room is invisible and closed, with four players, zero player/empty-room TTL. The already-joined client is passed to `StartGameArgs.RealtimeClient` with `GameMode.Host`; the room opens only after host startup succeeds.

Join normalizes the supplied code and calls `StartGame` with `GameMode.Client` and `EnableClientSessionCreation = false`. Both paths use the configured Photon settings; no App ID is supplied by code. Codes are scoped to the Photon application/version and region: use the same fixed region on all instances for testing.

Host membership callbacks map Fusion PlayerRefs to session IDs and fixed seat indices 0–3. Placeholder names and colors follow seats. A departed lobby client's seat reopens without renumbering anyone else; a replacement starts unready. The creator never transfers its host role. Host loss ends the session. Cloud quick rejoin is disabled and no migration resume handler exists.

The host sends small JSON lobby snapshots through Fusion reliable data. Clients send only snapshot requests and ready booleans. Sender identity comes from Fusion, not the JSON. Snapshot revisions reject older deliveries; ready commands use per-sender sequence checks. New clients request a full snapshot after their runner starts. No network prefab or scene rewrite is needed. StartGame references the existing Phase2Multiplayer scene from the build scene list, which also permits the opt-in multi-peer smoke test. All participants must use the same scene list and order.

The host alone sees Start. Both UI and host domain require exactly four connected, ready players. Start locks the domain and closes `SessionInfo.IsOpen`; a connection racing that change is also checked in connect/join callbacks. Photon room capacity and domain checks reject a fifth player. The started screen explicitly states that gameplay networking is not implemented.

## Exact manual test: editor host plus three standalone clients

1. Open the project in Unity **6000.3.23f1**, allow imports to finish, and clear Console errors.
2. In Photon Realtime Settings, verify the existing Fusion App ID and select one available **Fixed Region** for every instance. Do not change the App ID or use different app versions between instances.
3. Open `Assets/_Project/Scenes/Phase2Multiplayer.unity`. Keep Fusion Peer Mode **Single** for this separate-process test.
4. Open **File > Build Profiles**, choose Windows, and set the scene list to contain **Phase2Multiplayer only** for the test build. Select **Development Build**, windowed mode, and **Run In Background**. Build to `Builds/Phase2Lobby/CrossDrive.exe`. Keep this same scene list active in the editor throughout testing so scene indices match the builds; restore your previous list only after the test is finished.
5. Launch that executable three times, each with `-screen-fullscreen 0 -screen-width 640 -screen-height 720`. These are clients A, B, C. Keep all instances running while switching focus.
6. Press Play in the editor and click **Create Room**. Wait for a six-character code, `Player 1`, and three empty seats. No second click should start a competing connection attempt.
7. On A, B, C click **Join Room**, enter that exact code, and click **Join**. All four instances must display identical seat/name/color/ready data. Start exists only on the editor host and remains disabled.
8. Ready only three players: Start stays disabled. Ready the fourth: Start enables. Toggle any client to Not Ready: Start disables again, then re-enables when ready. Clients must not have a Start button.
9. Launch a fourth standalone client (fifth participant) and join: expect **Room full**, with no fifth seat on any peer.
10. Before starting, leave client B. Verify its seat empties and surviving seats do not move. Join the probe into that vacancy: it must have the vacant seat and be unready. Ready all four again.
11. Click host **Start**. All four show **MATCH STARTED** and the milestone message. No cars, assignments, countdown, input, or scoring should appear.
12. Leave one client, then try joining the old code with the probe: expect room closed/match started despite fewer than four connected players.
13. Stop editor Play Mode or close the host. Remaining clients must return home with a host-left/host-connection-lost error. No client becomes host. Repeat host departure before Start as a separate run.
14. Try an unused valid room code: expect **Room not found**. Disconnect internet and attempt create/join: expect a useful connection failure, then restore connectivity and retry successfully.
15. Verify the Phase1Prototype scene still starts and retains its existing controls and scoring behavior.

For a deterministic create collision, run the opt-in automated smoke test below; it forces the existing creation path to reuse the host's code through test-only reflection. Normal UI creation always generates a new code.

## Automated checks

EditMode (close other editors using this project first):

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath D:/CrossDrive -runTests -testPlatform EditMode -testResults D:/CrossDrive/Logs/phase2-m1-tests.xml -logFile D:/CrossDrive/Logs/phase2-m1-unity.log
```

Opt-in live Photon smoke (contacts the configured application; requires available CCU and network access):

```powershell
$env:CROSSDRIVE_PHOTON_SMOKE = '1'
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath D:/CrossDrive -runTests -testPlatform PlayMode -testFilter CrossDrive.Tests.Phase2PhotonSmokeTests -testResults D:/CrossDrive/Logs/phase2-m1-photon.xml -logFile D:/CrossDrive/Logs/phase2-m1-photon.log
Remove-Item Env:CROSSDRIVE_PHOTON_SMOKE
```

The smoke test temporarily selects Multiple peer mode in memory, restoring it afterward. Ordinary test runs skip live connectivity. Expected Photon join-rejection error logs are allowed only within the smoke test; assertions check the resulting views and state.

## Verification record and gaps

- Final Unity 6000.3.23f1 compilation and 37/37 EditMode tests passed (2026-09-09 01:33 UTC). Evidence: `Logs/phase2-m1-tests.xml` and `Logs/phase2-m1-unity.log`. Live evidence: `Logs/phase2-m1-photon.xml` and `Logs/phase2-m1-photon.log`. Logs remain ignored by Git.
- Live Photon smoke passed with one host and three clients on the configured Photon application, in a headless multi-peer Unity PlayMode run. It exercised atomic create collision failure, fifth-player rejection, ready/unready replication, host-only start, replicated match-start marker, closed-room late-join rejection, and host departure. The final run also passed missing-room and stable-seat checks (2026-09-09 01:32 UTC, 19.77 seconds).
- Manual window/UI validation and separate-device connectivity are not established by EditMode checks.
- Neither `AGENTS.md` nor the requested `docs/PHASE2_MULTIPLAYER_SPEC.md` existed in the supplied working tree. This document records implementation and testing, not a replacement specification.

## Official API references

- [Fusion 2.1 custom Realtime client handoff](https://doc.photonengine.com/fusion/v2/manual/advanced/custom-realtime-client)
- [Fusion 2 matchmaking semantics](https://doc.photonengine.com/fusion/v2/manual/connection-and-matchmaking/matchmaking)
- [Realtime 5 asynchronous operations](https://doc.photonengine.com/realtime/v5/connection-and-authentication/async-extensions)
- [Fusion reliable data streaming](https://doc.photonengine.com/fusion/v2/manual/data-transfer/data-streaming)

Signatures were checked against the installed `Fusion.Runtime.xml`, `Fusion.Sockets.xml`, Fusion Unity source, and Realtime `AsyncExtensions.cs`, then compiled by the installed Unity editor. Fusion 2.1.2 uses `ReadOnlySpan<byte>` for the reliable-data callback.
