using System.Linq;
using CrossDrive.Networking;
using NUnit.Framework;

namespace CrossDrive.Tests
{
    public sealed class Phase2LobbyTests
    {
        private static RoomService FullRoom()
        {
            var room = new RoomService();
            room.CreateRoom("ABCDEF", "host", null);
            for (int i = 1; i < 4; i++) room.TryJoin("p" + i, null, out _);
            return room;
        }

        [Test]
        public void ReadyUnreadyAndHostIdentityGateStart()
        {
            var room = FullRoom();
            foreach (var p in room.Players) room.TrySetReady(p.SessionPlayerId, true);
            Assert.That(room.CanHostStart("host"), Is.True);
            Assert.That(room.TryMarkMatchStarted("p1"), Is.False);
            Assert.That(room.TryMarkMatchStarted("stranger"), Is.False);
            room.TrySetReady("p2", false);
            Assert.That(room.TryMarkMatchStarted("host"), Is.False);
            room.TrySetReady("p2", true);
            Assert.That(room.TryMarkMatchStarted("host"), Is.True);
            Assert.That(room.TrySetReady("p2", false), Is.False);
            Assert.That(room.TryMarkMatchStarted("host"), Is.False);
        }

        [Test]
        public void VacantSeatIsReusedWithoutRenumberingSurvivors()
        {
            var room = FullRoom();
            var survivor = room.Players.Single(p => p.SessionPlayerId == "p3");
            room.TrySetReady("p1", true);
            room.Disconnect("p1");
            Assert.That(room.TryJoin("replacement", null, out var replacement), Is.EqualTo(RoomJoinResult.Accepted));
            Assert.That(replacement.SlotId, Is.EqualTo(1));
            Assert.That(replacement.DisplayName, Is.EqualTo("Player 2"));
            Assert.That(replacement.IsReady, Is.False);
            Assert.That(survivor.SlotId, Is.EqualTo(3));
            Assert.That(room.Players.Select(p => p.SlotId).Distinct().Count(), Is.EqualTo(4));
            Assert.That(room.TryJoin("fifth", null, out _), Is.EqualTo(RoomJoinResult.RoomFull));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DepartingHostNeverPromotesClientOrAllowsAnotherJoin(bool started)
        {
            var room = FullRoom();
            foreach (var p in room.Players) room.TrySetReady(p.SessionPlayerId, true);
            if (started) room.TryMarkMatchStarted("host");
            Assert.That(room.Disconnect("host"), Is.EqualTo(DisconnectResult.HostLeft));
            Assert.That(room.Players.Where(p => p.SessionPlayerId != "host").Any(p => p.IsHost), Is.False);
            Assert.That(room.CanHostStart("p1"), Is.False);
            Assert.That(room.CanHostStart("host"), Is.False);
            Assert.That(room.TrySetReady("p1", true), Is.False);
            Assert.That(room.TryJoin("new", null, out _), Is.EqualTo(RoomJoinResult.HostLeft));
        }

        [Test]
        public void StartedRoomRejectsJoinEvenWhenSeatBecomesDisconnected()
        {
            var room = FullRoom();
            foreach (var p in room.Players) room.TrySetReady(p.SessionPlayerId, true);
            room.TryMarkMatchStarted("host");
            room.Disconnect("p1");
            Assert.That(room.ActivePlayerCount, Is.EqualTo(3));
            Assert.That(room.TryJoin("late", null, out _), Is.EqualTo(RoomJoinResult.MatchInProgress));
            Assert.That(room.TryJoin("p1", null, out _), Is.EqualTo(RoomJoinResult.DuplicateSession));
        }

        [Test]
        public void GeneratedCodesAreShortAndUnambiguous()
        {
            for (int i = 0; i < 100; i++)
            {
                string code = FusionRoomTransport.GenerateRoomCode();
                Assert.That(code, Does.Match("^[ABCDEFGHJKMNPQRSTUVWXYZ23456789]{6}$"));
                Assert.That(RoomService.IsValidRoomCode(code), Is.True);
            }
        }
    }
}
