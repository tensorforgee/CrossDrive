using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CrossDrive.Networking;
using Fusion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrossDrive.Tests
{
    // Explicit environment opt-in: ordinary test runs never contact Photon.
    public sealed class Phase2PhotonSmokeTests
    {
        private readonly List<FusionRoomTransport> peers = new List<FusionRoomTransport>();
        private NetworkProjectConfig.PeerModes previousMode;
        private bool configured;

        [UnityTest]
        public IEnumerator HostAndThreeClientsLobbySmoke()
        {
            if (Environment.GetEnvironmentVariable("CROSSDRIVE_PHOTON_SMOKE") != "1")
                Assert.Ignore("Set CROSSDRIVE_PHOTON_SMOKE=1 to use the configured Photon application.");
            previousMode = NetworkProjectConfig.Global.PeerMode;
            NetworkProjectConfig.Global.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
            configured = true;
            // Photon reports expected rejected joins as errors; assert the resulting view below.
            LogAssert.ignoreFailingMessages = true;
            var host = AddPeer();
            Assert.That(host.IsConfigured, Is.True, host.ConfigurationMessage);
            host.CreateRoom(null);
            yield return WaitFor(() => !host.View.IsConnecting, "host creation", host);
            Assert.That(host.View.Phase, Is.EqualTo(NetworkGamePhase.Lobby), host.View.StatusMessage);
            string code = host.View.RoomCode;
            Debug.Log("PHOTON SMOKE: real host created.");

            var probe = AddPeer();
            // Force a collision through the same private create path, without adding a production override.
            typeof(FusionRoomTransport).GetMethod("Begin", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(probe, new object[] { true, code });
            yield return WaitFor(() => !probe.View.IsConnecting, "create collision", probe);
            Assert.That(probe.View.Phase, Is.EqualTo(NetworkGamePhase.Home));
            Assert.That(probe.View.StatusMessage, Does.Contain("already exists"));
            Assert.That(host.View.Players.Count, Is.EqualTo(1));

            probe.JoinRoom(FusionRoomTransport.GenerateRoomCode(), null);
            yield return WaitFor(() => !probe.View.IsConnecting, "missing room", probe);
            Assert.That(probe.View.Phase, Is.EqualTo(NetworkGamePhase.Home));
            Assert.That(probe.View.StatusMessage, Does.Contain("not found"));

            for (int i = 0; i < 3; i++)
            {
                var peer = AddPeer();
                peer.JoinRoom(code.ToLowerInvariant(), null);
                yield return WaitFor(() => peer.View.Phase == NetworkGamePhase.Lobby, "client join", peer);
            }
            var active = peers.Where(p => p != probe).ToArray();
            yield return WaitFor(() => active.All(p => p.View.Players.Count == 4), "four seats", host);
            Debug.Log("PHOTON SMOKE: one host and three real clients connected; four seats on all peers.");
            probe.JoinRoom(code, null);
            yield return WaitFor(() => !probe.View.IsConnecting, "fifth join", probe);
            Assert.That(probe.View.StatusMessage, Does.Contain("full"));

            string survivorId = active[2].View.LocalSessionPlayerId;
            int survivorSeat = host.View.Players.Single(p => p.SessionPlayerId == survivorId).SlotId;
            int vacantSeat = host.View.Players.Single(p => p.SessionPlayerId == active[3].View.LocalSessionPlayerId).SlotId;
            active[3].LeaveRoom();
            yield return WaitFor(() => host.View.Players.Count == 3 && !active[3].View.IsConnecting, "lobby vacancy", host);
            active[3].JoinRoom(code, null); // New session, not reconnect restoration.
            yield return WaitFor(() => active.All(p => p.View.Players.Count == 4), "replacement seat", host);
            Assert.That(host.View.Players.Single(p => p.SessionPlayerId == survivorId).SlotId, Is.EqualTo(survivorSeat));
            Assert.That(host.View.Players.Single(p => p.SessionPlayerId == active[3].View.LocalSessionPlayerId).SlotId, Is.EqualTo(vacantSeat));
            Assert.That(active[3].View.Players.Single(p => p.SessionPlayerId == active[3].View.LocalSessionPlayerId).IsReady, Is.False);

            foreach (var peer in active) peer.SetReady(true);
            yield return WaitFor(() => host.View.CanHostStart, "ready replication", host);
            active[1].SetReady(false);
            yield return WaitFor(() => !host.View.CanHostStart, "unready replication", host);
            active[1].StartMatch();
            Assert.That(host.View.Phase, Is.EqualTo(NetworkGamePhase.Lobby));
            active[1].SetReady(true);
            yield return WaitFor(() => host.View.CanHostStart, "ready again", host);
            host.StartMatch();
            yield return WaitFor(() => active.All(p => p.View.Phase == NetworkGamePhase.MatchStarted), "start replication", host);
            active[3].LeaveRoom();
            yield return WaitFor(() => !active[3].View.IsConnecting, "client leave", active[3]);
            probe.JoinRoom(code, null);
            yield return WaitFor(() => !probe.View.IsConnecting, "late join", probe);
            Assert.That(probe.View.Phase, Is.EqualTo(NetworkGamePhase.Home));
            Assert.That(probe.View.StatusMessage, Does.Contain("closed").Or.Contain("started"));
            host.LeaveRoom();
            yield return WaitFor(() => active[1].View.Phase == NetworkGamePhase.Home, "host departure", active[1]);
            Assert.That(active[1].View.StatusMessage, Does.Contain("Host left"));
            Debug.Log("PHOTON SMOKE PASSED: collision, missing room, capacity, stable seats, ready/unready, host start, late join and host departure.");
        }

        private FusionRoomTransport AddPeer()
        {
            var peer = new GameObject("Photon smoke peer").AddComponent<FusionRoomTransport>();
            peers.Add(peer);
            return peer;
        }
        private static IEnumerator WaitFor(Func<bool> predicate, string step, FusionRoomTransport peer)
        {
            float until = Time.realtimeSinceStartup + 45f;
            while (!predicate() && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(predicate(), Is.True, step + ": " + peer.View.StatusMessage);
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (var peer in peers) if (peer != null) peer.LeaveRoom();
            float until = Time.realtimeSinceStartup + 10f;
            while (peers.Any(p => p != null && p.View.IsConnecting) && Time.realtimeSinceStartup < until) yield return null;
            foreach (var peer in peers) if (peer != null) UnityEngine.Object.Destroy(peer.gameObject);
            peers.Clear();
            if (configured) NetworkProjectConfig.Global.PeerMode = previousMode;
            LogAssert.ignoreFailingMessages = false;
        }
    }
}
