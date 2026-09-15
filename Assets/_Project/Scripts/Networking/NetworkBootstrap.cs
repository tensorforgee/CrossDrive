using System;
using UnityEngine;

namespace CrossDrive.Networking
{
    public sealed class NetworkBootstrap : MonoBehaviour
    {
        private readonly NetworkInputSource inputSource = new NetworkInputSource();
        private IRoomTransport room;
        private string displayName = "Player";
        private string roomCode = string.Empty;
        private bool showJoin;
        private bool boostQueued;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle warningStyle;

        public void Initialize(IRoomTransport roomTransport)
        {
            if (room != null) room.ViewChanged -= RepaintView;
            room = roomTransport ?? throw new ArgumentNullException(nameof(roomTransport));
            room.ViewChanged += RepaintView;
        }

        private void Awake()
        {
            if (room == null) Initialize(gameObject.AddComponent<FusionRoomTransport>());
        }

        private void OnDestroy()
        {
            if (room != null) room.ViewChanged -= RepaintView;
        }

        private void OnGUI()
        {
            EnsureStyles();
            NetworkClientView view = room.View;
            float panelWidth = Mathf.Min(560f, Mathf.Max(280f, Screen.width - 40f));
            Rect panel = new Rect(Mathf.Max(20f, Screen.width * 0.5f - panelWidth * 0.5f), 30f,
                panelWidth, Mathf.Max(420f, Screen.height - 60f));
            GUI.Box(panel, GUIContent.none);
            GUILayout.BeginArea(new Rect(panel.x + 24f, panel.y + 20f, panel.width - 48f, panel.height - 40f));
            GUILayout.Label("CROSSDRIVE — PHASE 2 MULTIPLAYER", titleStyle);
            GUILayout.Space(12f);

            if (!room.IsConfigured)
            {
                GUILayout.Label(room.ConfigurationMessage, warningStyle);
                GUILayout.Space(16f);
            }

            switch (view.Phase)
            {
                case NetworkGamePhase.Home:
                    DrawHome();
                    break;
                case NetworkGamePhase.Lobby:
                    DrawLobby(view);
                    break;
                case NetworkGamePhase.Results:
                    DrawGame(view, true);
                    break;
                case NetworkGamePhase.MatchStarted:
                    GUILayout.Label("MATCH STARTED", titleStyle);
                    if (GUILayout.Button("Leave")) room.LeaveRoom();
                    break;
                case NetworkGamePhase.Interrupted:
                    GUILayout.Label("MATCH ENDED SAFELY", titleStyle);
                    GUILayout.Label(view.StatusMessage, warningStyle);
                    if (GUILayout.Button("Leave")) room.LeaveRoom();
                    break;
                default:
                    DrawGame(view, false);
                    break;
            }

            if (!string.IsNullOrEmpty(view.StatusMessage))
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(view.StatusMessage, warningStyle);
            }
            GUILayout.EndArea();
        }

        private void DrawHome()
        {
            GUILayout.Label("Seats use placeholder names Player 1–4.", bodyStyle);
            GUILayout.Space(12f);
            GUI.enabled = room.IsConfigured && !room.View.IsConnecting;
            if (GUILayout.Button("Create Room", GUILayout.Height(48f))) room.CreateRoom(displayName);
            if (GUILayout.Button("Join Room", GUILayout.Height(48f))) showJoin = true;
            GUI.enabled = true;

            if (showJoin)
            {
                GUILayout.Space(16f);
                GUILayout.Label("Room code", bodyStyle);
                roomCode = GUILayout.TextField(roomCode.ToUpperInvariant(), 8);
                GUI.enabled = room.IsConfigured && !room.View.IsConnecting && RoomService.IsValidRoomCode(roomCode);
                if (GUILayout.Button("Join", GUILayout.Height(42f))) room.JoinRoom(RoomService.NormalizeRoomCode(roomCode), displayName);
                GUI.enabled = true;
            }
        }

        private void DrawLobby(NetworkClientView view)
        {
            GUILayout.Label($"ROOM {view.RoomCode}", titleStyle);
            GUILayout.Label($"PLAYERS {view.Players.Count}/4", bodyStyle);
            for (int seat = 0; seat < RoomService.RequiredPlayerCount; seat++)
            {
                NetworkPlayer player = null;
                foreach (var candidate in view.Players) if (candidate.SlotId == seat) player = candidate;
                if (player == null) { GUILayout.Label($"Seat {seat + 1} — waiting for player", bodyStyle); continue; }
                string host = player.IsHost ? "  HOST" : string.Empty;
                string ready = player.IsReady ? "READY" : "NOT READY";
                GUILayout.Label($"{player.DisplayName} — {player.CarColorName} — {ready}{host}", bodyStyle);
            }
            GUILayout.Space(14f);
            NetworkPlayer local = FindLocal(view);
            if (local != null && GUILayout.Button(local.IsReady ? "Not Ready" : "Ready")) room.SetReady(!local.IsReady);
            if (local != null && local.IsHost)
            {
                GUI.enabled = view.CanHostStart;
                if (GUILayout.Button("Start (Host)", GUILayout.Height(44f))) room.StartMatch();
                GUI.enabled = true;
            }
            if (GUILayout.Button("Leave")) room.LeaveRoom();
        }

        private void DrawGame(NetworkClientView view, bool results)
        {
            GUILayout.Label(results ? $"RESULTS — {view.ScoringMode}" :
                $"{view.Phase.ToString().ToUpperInvariant()} — {view.ScoringMode}", titleStyle);
            if (view.PhaseEndsAt > 0d) GUILayout.Label($"Host timer ends at {view.PhaseEndsAt:0.000}", bodyStyle);

            if (view.PrivateAssignment != null)
            {
                PrivateAssignmentView assignment = view.PrivateAssignment;
                GUILayout.Label($"MY OWN CAR: {assignment.OwnCarColorName}", bodyStyle);
                GUILayout.Label($"CAR I CONTROL: {assignment.ControlledCarColorName}", bodyStyle);
                GUILayout.Label($"OWNER OF CAR I CONTROL: {assignment.ControlledCarOwnerDisplayName}", bodyStyle);
                GUILayout.Label($"MY DRIVER: {assignment.OwnCarDriverDisplayName}", bodyStyle);
            }

            if (view.RevealedDriverToCarOwnerMap != null)
            {
                GUILayout.Space(8f);
                GUILayout.Label("FULL DRIVER MAPPING", titleStyle);
                for (int driver = 0; driver < view.RevealedDriverToCarOwnerMap.Length; driver++)
                    GUILayout.Label($"Player {driver + 1} drives car {view.RevealedDriverToCarOwnerMap[driver] + 1}", bodyStyle);
            }

            if (view.Scores != null && view.Scores.Count > 0)
            {
                GUILayout.Space(8f);
                for (int player = 0; player < view.Scores.Count; player++) GUILayout.Label($"Player {player + 1}: {view.Scores[player]}", bodyStyle);
            }

            if (results)
            {
                GUI.enabled = view.CanRematch;
                if (GUILayout.Button("Rematch")) room.RequestRematch();
                GUI.enabled = true;
                if (GUILayout.Button("Leave")) room.LeaveRoom();
                return;
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            bool left = GUILayout.RepeatButton("◀", GUILayout.Height(70f));
            if (GUILayout.Button("BOOST", GUILayout.Height(70f))) boostQueued = true;
            bool right = GUILayout.RepeatButton("▶", GUILayout.Height(70f));
            GUILayout.EndHorizontal();
            if (Event.current.type == EventType.Repaint)
            {
                room.SubmitInput(inputSource.Capture(left, right, boostQueued));
                boostQueued = false;
            }
        }

        private static NetworkPlayer FindLocal(NetworkClientView view)
        {
            foreach (NetworkPlayer player in view.Players)
                if (player.SessionPlayerId == view.LocalSessionPlayerId) return player;
            return null;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            warningStyle = new GUIStyle(bodyStyle) { normal = { textColor = new Color(1f, 0.65f, 0.25f) } };
        }

        private void RepaintView() { }
    }
}
