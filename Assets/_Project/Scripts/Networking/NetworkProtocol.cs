using System;
using System.Collections.Generic;
using CrossDrive.Scoring;

namespace CrossDrive.Networking
{
    public enum NetworkGamePhase
    {
        Home,
        Lobby,
        Assignment,
        Countdown,
        Anonymous,
        Reveal,
        Known,
        Results,
        Interrupted,
        MatchStarted,
    }

    public enum NetworkPlayerConnectionState
    {
        Connecting,
        Connected,
        Disconnected,
    }

    public enum RoomJoinResult
    {
        Accepted,
        InvalidRoomCode,
        DuplicateSession,
        RoomFull,
        MatchInProgress,
        HostLeft,
    }

    public enum DisconnectResult
    {
        NotFound,
        HostLeft,
        RemovedFromLobby,
        MatchInterrupted,
    }

    public readonly struct NetworkInputCommand
    {
        public NetworkInputCommand(float steer, bool boostPressed, uint sequence)
        {
            Steer = Math.Max(-1f, Math.Min(1f, steer));
            BoostPressed = boostPressed;
            Sequence = sequence;
        }

        public float Steer { get; }
        public bool BoostPressed { get; }
        public uint Sequence { get; }
    }

    public sealed class NetworkPlayer
    {
        internal NetworkPlayer(string sessionPlayerId, int slotId, string displayName, string carColorName, bool isHost)
        {
            SessionPlayerId = sessionPlayerId;
            SlotId = slotId;
            DisplayName = displayName;
            OwnedCarId = slotId;
            CarColorName = carColorName;
            IsHost = isHost;
            ConnectionState = NetworkPlayerConnectionState.Connected;
        }

        public string SessionPlayerId { get; }
        public int SlotId { get; }
        public string DisplayName { get; }
        public int OwnedCarId { get; }
        public string CarColorName { get; }
        public bool IsHost { get; private set; }
        public bool IsReady { get; private set; }
        public NetworkPlayerConnectionState ConnectionState { get; private set; }

        internal void SetReady(bool ready) => IsReady = ready;
        internal void SetHost(bool host) => IsHost = host;
        internal void SetConnectionState(NetworkPlayerConnectionState state) => ConnectionState = state;
    }

    public sealed class NetworkClientView
    {
        public NetworkGamePhase Phase = NetworkGamePhase.Home;
        public string RoomCode = string.Empty;
        public string LocalSessionPlayerId = string.Empty;
        public string StatusMessage = string.Empty;
        public double PhaseEndsAt;
        public bool CanHostStart;
        public bool IsConnecting;
        public bool CanRematch;
        public ScoringMode ScoringMode = ScoringMode.Siphon;
        public PrivateAssignmentView PrivateAssignment;
        public int[] RevealedDriverToCarOwnerMap;
        public IReadOnlyList<NetworkPlayer> Players = Array.Empty<NetworkPlayer>();
        public IReadOnlyList<int> Scores = Array.Empty<int>();
    }

    public interface IRoomTransport
    {
        bool IsConfigured { get; }
        string ConfigurationMessage { get; }
        NetworkClientView View { get; }
        event Action ViewChanged;
        void CreateRoom(string displayName);
        void JoinRoom(string roomCode, string displayName);
        void SetReady(bool ready);
        void StartMatch();
        void SubmitInput(NetworkInputCommand input);
        void RequestRematch();
        void LeaveRoom();
    }
}
