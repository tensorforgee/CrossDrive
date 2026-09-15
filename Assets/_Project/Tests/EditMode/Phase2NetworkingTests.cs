using System;
using System.Linq;
using CrossDrive.Gameplay;
using CrossDrive.Networking;
using CrossDrive.Scoring;
using NUnit.Framework;

namespace CrossDrive.Tests
{
    public sealed class Phase2NetworkingTests
    {
        [Test]
        public void HostStartGeneratesCompleteFourPlayerCycleAndDefaultsToSiphon()
        {
            NetworkSessionCoordinator session = CreateReadySession();

            Assert.That(session.TryHostStart("host", 10d, 1234, out string error), Is.True, error);
            Assert.That(ControlAssignmentService.IsValidCompleteCycle(session.Round.Assignment.CopyDriverToCarOwnerMap()), Is.True);
            Assert.That(session.Round.ScoringMode, Is.EqualTo(ScoringMode.Siphon));
            Assert.That(session.Round.Phase, Is.EqualTo(NetworkGamePhase.Assignment));
        }

        [Test]
        public void PrivateViewContainsOnlyOwnAndControlledCarInformation()
        {
            NetworkSessionCoordinator session = CreateReadySession();
            session.TryHostStart("host", 10d, 7, out _);

            PrivateAssignmentView view = session.Round.CreatePrivateAssignmentView(true, "p2");

            Assert.That(view.SessionPlayerId, Is.EqualTo("p2"));
            Assert.That(view.OwnCarId, Is.EqualTo(2));
            Assert.That(view.ControlledCarId, Is.Not.EqualTo(view.OwnCarId));
            Assert.That(view.ControlledCarOwnerSessionPlayerId, Is.Not.Empty);
            Assert.That(view.OwnCarDriverDisplayName, Is.EqualTo("???"));
            Assert.That(session.Round.CreateSnapshot().RevealedDriverToCarOwnerMap, Is.Null);
        }

        [Test]
        public void RevealPublishesFullMappingAndKnownPhaseKeepsSameMapping()
        {
            NetworkSessionCoordinator session = CreateReadySession();
            session.TryHostStart("host", 100d, 44, out _);
            int[] original = session.Round.Assignment.CopyDriverToCarOwnerMap();
            session.Round.ConfirmPrivateAssignmentsDelivered(true, 100d);
            session.Round.Tick(true, 144d);

            Assert.That(session.Round.Phase, Is.EqualTo(NetworkGamePhase.Reveal));
            Assert.That(session.Round.CreateSnapshot().RevealedDriverToCarOwnerMap, Is.EqualTo(original));

            session.Round.Tick(true, 148d);
            Assert.That(session.Round.Phase, Is.EqualTo(NetworkGamePhase.Known));
            Assert.That(session.Round.CreateSnapshot().RevealedDriverToCarOwnerMap, Is.EqualTo(original));
        }

        [Test]
        public void GemPickupScoresExactlyOnceForOneSpawnGeneration()
        {
            NetworkRoundCoordinator round = StartAnonymousRound();

            Assert.That(round.TryCollectGem(true, 0, 0, 1, 105d, out var firstDeltas), Is.True);
            Assert.That(firstDeltas, Is.Not.Empty);
            int[] afterFirst = round.Scores.ToArray();
            Assert.That(round.TryCollectGem(true, 0, 0, 1, 105.1d, out var repeatedDeltas), Is.False);
            Assert.That(repeatedDeltas, Is.Empty);
            Assert.That(round.Scores, Is.EqualTo(afterFirst));

            round.Tick(true, 107d);
            Assert.That(round.Gems[0].IsVisible, Is.True);
            Assert.That(round.Gems[0].Generation, Is.EqualTo(1));
        }

        [Test]
        public void PitEntryScoresExactlyOncePerCarLifeAndRespawnsAfterThreeSeconds()
        {
            NetworkRoundCoordinator round = StartAnonymousRound();
            round.TryConfigureCarSpawn(true, 1, 6.2f, 3.4f, 90f);

            Assert.That(round.TryEnterPit(true, 1, 0, -1, -1f, 110d, out var firstDeltas), Is.True);
            Assert.That(firstDeltas, Is.Not.Empty);
            int[] afterFirst = round.Scores.ToArray();
            Assert.That(round.TryEnterPit(true, 1, 0, -1, -1f, 110.1d, out var repeatedDeltas), Is.False);
            Assert.That(repeatedDeltas, Is.Empty);
            Assert.That(round.Scores, Is.EqualTo(afterFirst));
            Assert.That(round.Cars[1].IsRespawning, Is.True);

            round.Tick(true, 113d);
            Assert.That(round.Cars[1].IsRespawning, Is.False);
            Assert.That(round.Cars[1].LifeId, Is.EqualTo(1));
            Assert.That(round.Cars[1].PositionX, Is.EqualTo(6.2f));
            Assert.That(round.Cars[1].PositionY, Is.EqualTo(3.4f));
            Assert.That(round.Cars[1].RotationDegrees, Is.EqualTo(90f));
        }

        [Test]
        public void OnlyAuthorityAdvancesNetworkPhasesAndUsesAuthoritativeTimestamps()
        {
            NetworkSessionCoordinator session = CreateReadySession();
            session.TryHostStart("host", 100d, 3, out _);
            session.Round.ConfirmPrivateAssignmentsDelivered(true, 100d);

            Assert.That(session.Round.Tick(false, 999d), Is.False);
            Assert.That(session.Round.Phase, Is.EqualTo(NetworkGamePhase.Countdown));
            session.Round.Tick(true, 104d);
            Assert.That(session.Round.Phase, Is.EqualTo(NetworkGamePhase.Anonymous));
            Assert.That(session.Round.PhaseEndsAt, Is.EqualTo(144d));
            session.Round.Tick(true, 183d);
            Assert.That(session.Round.Phase, Is.EqualTo(NetworkGamePhase.Results));
            Assert.That(session.Round.PhaseEndsAt, Is.EqualTo(191d));
            Assert.That(session.TryHostRematch("host", 190.9d, 4, out _), Is.False,
                "Results must remain visible for the configured eight seconds.");
            Assert.That(session.TryHostRematch("host", 191d, 4, out _), Is.True);
            Assert.That(session.Round.Phase, Is.EqualTo(NetworkGamePhase.Assignment));
        }

        [Test]
        public void RoomCannotStartUnderFourAndRejectsFifthActivePlayer()
        {
            RoomService room = new RoomService();
            room.CreateRoom("AB12", "host", "Host");
            room.TryJoin("p1", "Player 2", out _);
            room.TryJoin("p2", "Player 3", out _);
            room.TrySetReady("host", true);
            room.TrySetReady("p1", true);
            room.TrySetReady("p2", true);

            Assert.That(room.CanHostStart("host"), Is.False);
            Assert.That(room.TryJoin("p3", "Player 4", out _), Is.EqualTo(RoomJoinResult.Accepted));
            Assert.That(room.TryJoin("p4", "Player 5", out _), Is.EqualTo(RoomJoinResult.RoomFull));
            Assert.That(room.ActivePlayerCount, Is.EqualTo(4));
        }

        [Test]
        public void LobbyDisconnectRemovesPlayerAndMatchDisconnectInterruptsRound()
        {
            RoomService lobby = new RoomService();
            lobby.CreateRoom("ROOM", "host", "Host");
            lobby.TryJoin("p1", "Player 2", out _);
            Assert.That(lobby.Disconnect("p1"), Is.EqualTo(DisconnectResult.RemovedFromLobby));
            Assert.That(lobby.ActivePlayerCount, Is.EqualTo(1));

            NetworkSessionCoordinator session = CreateReadySession();
            session.TryHostStart("host", 0d, 9, out _);
            Assert.That(session.Disconnect("p2"), Is.EqualTo(DisconnectResult.MatchInterrupted));
            Assert.That(session.Round.Phase, Is.EqualTo(NetworkGamePhase.Interrupted));
            Assert.That(session.Round.StatusMessage, Does.Contain("disconnected"));
        }

        [Test]
        public void SnapshotsCopyAuthoritativeScoresRatherThanSharingMutableState()
        {
            NetworkRoundCoordinator round = StartAnonymousRound();
            round.TryCollectGem(true, 2, 0, 3, 108d, out _);

            NetworkMatchSnapshot first = round.CreateSnapshot();
            NetworkMatchSnapshot second = round.CreateSnapshot();
            Assert.That(second.Scores, Is.EqualTo(first.Scores));
            first.Scores[0] = 9999;
            Assert.That(round.Scores[0], Is.Not.EqualTo(9999));
            Assert.That(second.Scores[0], Is.Not.EqualTo(9999));
        }

        [Test]
        public void InputRoutingTargetsControlledCarAndRejectsStaleCommands()
        {
            NetworkSessionCoordinator session = CreateReadySession();
            session.TryHostStart("host", 0d, 22, out _);
            int expectedCar = session.Round.Assignment.GetControlledCarOwner(0);
            Assert.That(session.Round.ResolveControlledCar("host"), Is.EqualTo(expectedCar));
            Assert.That(expectedCar, Is.Not.EqualTo(0));

            NetworkInputBuffer buffer = new NetworkInputBuffer();
            Assert.That(buffer.TrySubmit("host", new NetworkInputCommand(0.5f, false, 2)), Is.True);
            Assert.That(buffer.TrySubmit("host", new NetworkInputCommand(-1f, true, 1)), Is.False);
            Assert.That(buffer.TryGetLatest("host", out NetworkInputCommand latest), Is.True);
            Assert.That(latest.Sequence, Is.EqualTo(2));
        }

        private static NetworkSessionCoordinator CreateReadySession()
        {
            NetworkSessionCoordinator session = new NetworkSessionCoordinator();
            session.Room.CreateRoom("AB12", "host", "Host");
            session.Room.TryJoin("p1", "Player 2", out _);
            session.Room.TryJoin("p2", "Player 3", out _);
            session.Room.TryJoin("p3", "Player 4", out _);
            foreach (NetworkPlayer player in session.Room.Players) session.Room.TrySetReady(player.SessionPlayerId, true);
            return session;
        }

        private static NetworkRoundCoordinator StartAnonymousRound()
        {
            NetworkSessionCoordinator session = CreateReadySession();
            session.TryHostStart("host", 100d, 55, out _);
            session.Round.ConfirmPrivateAssignmentsDelivered(true, 100d);
            session.Round.Tick(true, 104d);
            return session.Round;
        }
    }
}
