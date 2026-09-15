using System;
using System.Collections.Generic;
using System.Linq;

namespace CrossDrive.Networking
{
    public sealed class RoomService
    {
        public const int RequiredPlayerCount = 4;
        private static readonly string[] CarColors = { "Red", "Cyan", "Yellow", "Magenta" };
        private readonly List<NetworkPlayer> players = new List<NetworkPlayer>();

        public string RoomCode { get; private set; } = string.Empty;
        public bool IsMatchRunning { get; private set; }
        public bool HasHostLeft { get; private set; }
        public IReadOnlyList<NetworkPlayer> Players => players;
        public int ActivePlayerCount => players.Count(player => player.ConnectionState == NetworkPlayerConnectionState.Connected);

        public void CreateRoom(string roomCode, string hostSessionPlayerId, string displayName)
        {
            if (!IsValidRoomCode(roomCode)) throw new ArgumentException("Room codes must contain 4-8 letters or digits.", nameof(roomCode));
            if (string.IsNullOrWhiteSpace(hostSessionPlayerId)) throw new ArgumentException("A stable session player id is required.", nameof(hostSessionPlayerId));

            players.Clear();
            HasHostLeft = false;
            RoomCode = NormalizeRoomCode(roomCode);
            IsMatchRunning = false;
            AddPlayer(hostSessionPlayerId, displayName, true);
        }

        public RoomJoinResult TryJoin(string sessionPlayerId, string displayName, out NetworkPlayer player)
        {
            player = null;
            if (HasHostLeft) return RoomJoinResult.HostLeft;
            if (!IsValidRoomCode(RoomCode)) return RoomJoinResult.InvalidRoomCode;
            if (players.Any(value => value.SessionPlayerId == sessionPlayerId)) return RoomJoinResult.DuplicateSession;
            if (IsMatchRunning) return RoomJoinResult.MatchInProgress;
            if (ActivePlayerCount >= RequiredPlayerCount) return RoomJoinResult.RoomFull;

            player = AddPlayer(sessionPlayerId, displayName, false);
            return RoomJoinResult.Accepted;
        }

        public bool TrySetReady(string sessionPlayerId, bool ready)
        {
            NetworkPlayer player = FindConnected(sessionPlayerId);
            if (player == null || IsMatchRunning || HasHostLeft) return false;
            player.SetReady(ready);
            return true;
        }

        public bool CanHostStart(string requesterSessionPlayerId)
        {
            NetworkPlayer requester = FindConnected(requesterSessionPlayerId);
            return !HasHostLeft && !IsMatchRunning && requester != null && requester.IsHost &&
                   ActivePlayerCount == RequiredPlayerCount && players.All(player => player.IsReady);
        }

        public bool TryMarkMatchStarted(string requesterSessionPlayerId)
        {
            if (!CanHostStart(requesterSessionPlayerId)) return false;
            IsMatchRunning = true;
            return true;
        }

        public bool CanHostRematch(string requesterSessionPlayerId)
        {
            NetworkPlayer requester = FindConnected(requesterSessionPlayerId);
            return !HasHostLeft && IsMatchRunning && requester != null && requester.IsHost &&
                   ActivePlayerCount == RequiredPlayerCount;
        }

        public void MarkReturnedToLobby()
        {
            IsMatchRunning = false;
            foreach (NetworkPlayer player in players) player.SetReady(false);
        }

        public DisconnectResult Disconnect(string sessionPlayerId)
        {
            NetworkPlayer player = players.FirstOrDefault(value => value.SessionPlayerId == sessionPlayerId);
            if (player == null) return DisconnectResult.NotFound;

            if (player.IsHost)
            {
                HasHostLeft = true;
                player.SetConnectionState(NetworkPlayerConnectionState.Disconnected);
                player.SetReady(false);
                return DisconnectResult.HostLeft;
            }

            if (IsMatchRunning)
            {
                player.SetConnectionState(NetworkPlayerConnectionState.Disconnected);
                player.SetReady(false);
                return DisconnectResult.MatchInterrupted;
            }

            players.Remove(player);
            return DisconnectResult.RemovedFromLobby;
        }

        public static bool IsValidRoomCode(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return false;
            string normalized = NormalizeRoomCode(roomCode);
            if (normalized.Length < 4 || normalized.Length > 8) return false;
            for (int index = 0; index < normalized.Length; index++)
            {
                if (!char.IsLetterOrDigit(normalized[index])) return false;
            }
            return true;
        }

        public static string NormalizeRoomCode(string roomCode) => (roomCode ?? string.Empty).Trim().ToUpperInvariant();

        private NetworkPlayer AddPlayer(string sessionPlayerId, string displayName, bool isHost)
        {
            int slot = FindOpenSlot();
            NetworkPlayer player = new NetworkPlayer(
                sessionPlayerId,
                slot,
                string.IsNullOrWhiteSpace(displayName) ? $"Player {slot + 1}" : displayName.Trim(),
                CarColors[slot],
                isHost);
            players.Add(player);
            players.Sort((left, right) => left.SlotId.CompareTo(right.SlotId));
            return player;
        }

        private int FindOpenSlot()
        {
            for (int slot = 0; slot < RequiredPlayerCount; slot++)
            {
                if (players.All(player => player.SlotId != slot)) return slot;
            }
            throw new InvalidOperationException("The room has no free active-player slot.");
        }

        private NetworkPlayer FindConnected(string sessionPlayerId)
        {
            return players.FirstOrDefault(value => value.SessionPlayerId == sessionPlayerId &&
                                                   value.ConnectionState == NetworkPlayerConnectionState.Connected);
        }
    }
}
