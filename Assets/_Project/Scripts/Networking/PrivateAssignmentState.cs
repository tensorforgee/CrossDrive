using System;
using System.Collections.Generic;
using System.Linq;
using CrossDrive.Gameplay;

namespace CrossDrive.Networking
{
    public sealed class PrivateAssignmentView
    {
        internal PrivateAssignmentView(NetworkPlayer player, NetworkPlayer controlledCarOwner)
        {
            SessionPlayerId = player.SessionPlayerId;
            OwnCarId = player.OwnedCarId;
            OwnCarColorName = player.CarColorName;
            ControlledCarId = controlledCarOwner.OwnedCarId;
            ControlledCarColorName = controlledCarOwner.CarColorName;
            ControlledCarOwnerSessionPlayerId = controlledCarOwner.SessionPlayerId;
            ControlledCarOwnerDisplayName = controlledCarOwner.DisplayName;
        }

        public string SessionPlayerId { get; }
        public int OwnCarId { get; }
        public string OwnCarColorName { get; }
        public int ControlledCarId { get; }
        public string ControlledCarColorName { get; }
        public string ControlledCarOwnerSessionPlayerId { get; }
        public string ControlledCarOwnerDisplayName { get; }
        public string OwnCarDriverDisplayName => "???";
    }

    public sealed class PrivateAssignmentState
    {
        private readonly ControlAssignment assignment;
        private readonly IReadOnlyList<NetworkPlayer> players;
        private int[] revealedDriverToCarOwnerMap;

        public PrivateAssignmentState(ControlAssignment controlAssignment, IReadOnlyList<NetworkPlayer> roomPlayers)
        {
            assignment = controlAssignment ?? throw new ArgumentNullException(nameof(controlAssignment));
            players = roomPlayers ?? throw new ArgumentNullException(nameof(roomPlayers));
            if (players.Count != assignment.PlayerCount) throw new ArgumentException("Player and assignment counts must match.", nameof(roomPlayers));
        }

        public bool IsRevealed => revealedDriverToCarOwnerMap != null;

        public PrivateAssignmentView CreatePrivateView(string sessionPlayerId)
        {
            NetworkPlayer player = players.Single(value => value.SessionPlayerId == sessionPlayerId);
            int controlledCarId = assignment.GetControlledCarOwner(player.SlotId);
            NetworkPlayer controlledOwner = players.Single(value => value.OwnedCarId == controlledCarId);
            return new PrivateAssignmentView(player, controlledOwner);
        }

        public void PublishReveal()
        {
            if (revealedDriverToCarOwnerMap == null) revealedDriverToCarOwnerMap = assignment.CopyDriverToCarOwnerMap();
        }

        public int[] CopyRevealedDriverToCarOwnerMap()
        {
            return revealedDriverToCarOwnerMap == null ? null : (int[])revealedDriverToCarOwnerMap.Clone();
        }

        internal ControlAssignment Assignment => assignment;
    }
}
