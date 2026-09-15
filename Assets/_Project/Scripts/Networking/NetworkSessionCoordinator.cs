using System;
using CrossDrive.Scoring;

namespace CrossDrive.Networking
{
    public sealed class NetworkSessionCoordinator
    {
        public NetworkSessionCoordinator()
        {
            Room = new RoomService();
            Round = new NetworkRoundCoordinator();
        }

        public RoomService Room { get; }
        public NetworkRoundCoordinator Round { get; }

        public bool TryHostStart(string requesterSessionPlayerId, double networkTime, int randomSeed, out string error,
            ScoringMode scoringMode = ScoringMode.Siphon)
        {
            if (!Room.TryMarkMatchStarted(requesterSessionPlayerId))
            {
                error = "Only the host can start, and exactly four connected ready players are required.";
                return false;
            }

            if (!Round.BeginAssignment(true, Room.Players, randomSeed, scoringMode))
            {
                Room.MarkReturnedToLobby();
                error = "The authoritative assignment could not be created.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public bool TryHostRematch(string requesterSessionPlayerId, double networkTime, int randomSeed, out string error,
            ScoringMode scoringMode = ScoringMode.Siphon)
        {
            if (Round.Phase != NetworkGamePhase.Results || networkTime < Round.PhaseEndsAt ||
                !Room.CanHostRematch(requesterSessionPlayerId))
            {
                error = "Only the connected host can rematch from results with all four players present.";
                return false;
            }
            if (!Round.BeginAssignment(true, Room.Players, randomSeed, scoringMode))
            {
                error = "The authoritative rematch assignment could not be created.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        public DisconnectResult Disconnect(string sessionPlayerId)
        {
            DisconnectResult result = Room.Disconnect(sessionPlayerId);
            if (result == DisconnectResult.MatchInterrupted || result == DisconnectResult.HostLeft)
            {
                Round.Interrupt(true, "A player disconnected. The round was ended to preserve assignment and score integrity.");
            }
            return result;
        }
    }
}
