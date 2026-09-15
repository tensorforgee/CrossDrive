using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fusion;
using Fusion.Matchmaking;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrossDrive.Networking
{
    // Lobby only: no assignments, input, or gameplay replication.
    public sealed class FusionRoomTransport : MonoBehaviour, IRoomTransport
    {
        private NetworkRunner runner;
        private RealtimeClient client;
        private NetworkDelegates callbacks;
        private readonly NetworkSessionCoordinator session = new NetworkSessionCoordinator();
        private readonly NetworkClientView view = new NetworkClientView();
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool busy, destroying, hosting;
        private int revision, receivedRevision = -1, messageId;
        private string code;
        private readonly Dictionary<PlayerRef, int> readySequences = new Dictionary<PlayerRef, int>();
        public bool IsConfigured => PhotonAppSettings.TryGetGlobal(out var settings) &&
            !string.IsNullOrWhiteSpace(settings.AppSettings.AppIdFusion);
        public string ConfigurationMessage => "Configure the existing Fusion App ID in PhotonAppSettings.";
        public NetworkClientView View => view;
        public event Action ViewChanged;

        public static string GenerateRoomCode()
        {
            const string alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
            byte[] bytes = new byte[6];
            using (var random = System.Security.Cryptography.RandomNumberGenerator.Create()) random.GetBytes(bytes);
            return new string(bytes.Select(value => alphabet[value % alphabet.Length]).ToArray());
        }

        public void CreateRoom(string displayName) => Begin(true, GenerateRoomCode());
        public void JoinRoom(string roomCode, string displayName)
        {
            if (!RoomService.IsValidRoomCode(roomCode)) { Report("Enter a valid 4–8 character room code."); return; }
            Begin(false, RoomService.NormalizeRoomCode(roomCode));
        }

        private async void Begin(bool create, string roomCode)
        {
            if (busy || runner != null || destroying) return;
            if (!IsConfigured) { Report(ConfigurationMessage); return; }
            int sceneIndex = SceneUtility.GetBuildIndexByScenePath("Assets/_Project/Scenes/Phase2Multiplayer.unity");
            if (sceneIndex < 0) { Report("Include Phase2Multiplayer in the active build profile scene list before connecting."); return; }
            busy = view.IsConnecting = true;
            hosting = create;
            code = roomCode;
            revision = 0;
            receivedRevision = -1;
            readySequences.Clear();
            Report(create ? "Creating room…" : "Connecting to room…");
            try
            {
                if (create)
                {
                    // Named Host StartGame alone is create-or-join. Reserve atomically instead.
                    client = MatchmakingArgumentsExtensions.BuildRealtimeClient();
                    var config = AsyncConfig.CreateUnityAsyncConfig();
                    config.CancellationToken = lifetime.Token;
                    await client.ConnectUsingSettingsAsync(PhotonAppSettings.Global.AppSettings, config);
                    short result = await client.CreateAndJoinRoomAsync(new EnterRoomArgs
                    {
                        RoomName = code,
                        RoomOptions = new RoomOptions
                        {
                            MaxPlayers = RoomService.RequiredPlayerCount,
                            IsOpen = false, IsVisible = false,
                            PlayerTtl = 0, EmptyRoomTtl = 0,
                            Plugins = new[] { RealtimeClientExtensions.FusionPluginName }
                        }
                    }, false, config);
                    if (result != 0)
                    {
                        Report(result == ErrorCode.GameIdAlreadyExists
                            ? "Room code already exists. Create again for a new code."
                            : $"Room creation failed (Photon {result}). Check connection and settings.");
                        await Cleanup();
                        return;
                    }
                }
                lifetime.Token.ThrowIfCancellationRequested();
                runner = new GameObject("CrossDrive Fusion Runner").AddComponent<NetworkRunner>();
                runner.ProvideInput = false;
                NetworkRunner.CloudConnectionLostCurrentMode = NetworkRunner.CloudConnectionLostMode.Disabled;
                callbacks = new NetworkDelegates();
                callbacks.OnPlayerJoined = PlayerJoined;
                callbacks.OnPlayerLeft = PlayerLeft;
                callbacks.OnReliableDataReceived = Receive;
                callbacks.OnConnectRequest = (r, request, token) =>
                {
                    if (session.Room.IsMatchRunning || session.Room.ActivePlayerCount >= 4) request.Refuse();
                    else request.Accept();
                };
                callbacks.OnShutdown = (r, reason) =>
                {
                    if (r != runner || busy || destroying) return;
                    Report(hosting ? ErrorMessage(reason) : "Host left or the host connection was lost. The room has ended.");
                    LeaveAfterFailure();
                };
                callbacks.OnHostMigration = (r, token) =>
                {
                    Report("Host left. The room has ended; create or join a new room.");
                    LeaveAfterFailure();
                };
                runner.AddCallbacks(callbacks);
                var sceneInfo = new NetworkSceneInfo();
                sceneInfo.AddSceneRef(SceneRef.FromIndex(sceneIndex), LoadSceneMode.Additive);
                var start = await runner.StartGame(new StartGameArgs
                {
                    GameMode = create ? GameMode.Host : GameMode.Client,
                    SessionName = code, RealtimeClient = client,
                    EnableClientSessionCreation = false,
                    PlayerCount = RoomService.RequiredPlayerCount,
                    IsOpen = false, IsVisible = false,
                    Scene = sceneInfo,
                    StartGameCancellationToken = lifetime.Token
                });
                if (!start.Ok)
                {
                    Report(ErrorMessage(start.ShutdownReason));
                    await Cleanup();
                    return;
                }
                if (create)
                {
                    EnsureHost();
                    runner.SessionInfo.IsOpen = true;
                    Publish();
                }
                else Send(new Message { kind = 0 }); // Request snapshot after local startup.
            }
            catch (Exception exception)
            {
                if (!destroying) Report($"Connection failed ({exception.GetType().Name}). Check internet, Photon settings and region, then retry.");
                await Cleanup();
            }
            finally
            {
                busy = view.IsConnecting = false;
                if (!destroying) ViewChanged?.Invoke();
            }
        }

        private static string Id(PlayerRef player) => player.RawEncoded.ToString();
        private void EnsureHost()
        {
            if (session.Room.RoomCode != code || !session.Room.Players.Any(p => p.IsHost && p.SessionPlayerId == Id(runner.LocalPlayer)))
                session.Room.CreateRoom(code, Id(runner.LocalPlayer), null);
        }
        private void PlayerJoined(NetworkRunner r, PlayerRef player)
        {
            if (r != runner || !hosting) return;
            EnsureHost();
            if (player != r.LocalPlayer && session.Room.TryJoin(Id(player), null, out _) != RoomJoinResult.Accepted)
            {
                r.Disconnect(player);
                return;
            }
            Publish();
        }
        private void PlayerLeft(NetworkRunner r, PlayerRef player)
        {
            if (r != runner || !hosting) return;
            session.Room.Disconnect(Id(player));
            readySequences.Remove(player);
            Publish();
        }
        public void SetReady(bool ready)
        {
            if (busy || runner == null || view.Phase != NetworkGamePhase.Lobby) return;
            if (hosting) { session.Room.TrySetReady(Id(runner.LocalPlayer), ready); Publish(); }
            else Send(new Message { kind = 1, ready = ready });
        }
        public void StartMatch()
        {
            if (busy || runner == null || !hosting || !session.Room.TryMarkMatchStarted(Id(runner.LocalPlayer))) return;
            runner.SessionInfo.IsOpen = false;
            Publish();
        }
        public void SubmitInput(NetworkInputCommand input) { }
        public void RequestRematch() { }

        [Serializable] private sealed class Message
        {
            public int kind, revision;
            public bool ready, started;
            public Seat[] seats;
        }
        [Serializable] private sealed class Seat
        {
            public string id, name, color;
            public int slot;
            public bool host, ready, connected;
        }
        private void Send(Message message, PlayerRef? recipient = null)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(message));
            var key = ReliableKey.FromInts(0x43444C31, ++messageId, 0, 0);
            if (recipient.HasValue) runner.SendReliableDataToPlayer(recipient.Value, key, bytes);
            else runner.SendReliableDataToServer(key, bytes);
        }
        private void Receive(NetworkRunner r, PlayerRef sender, ReliableKey key, ReadOnlySpan<byte> bytes)
        {
            if (r != runner || bytes.Length > 4096) return;
            key.GetInts(out int protocol, out int sequence, out _, out _);
            if (protocol != 0x43444C31) return;
            Message message;
            try { message = JsonUtility.FromJson<Message>(Encoding.UTF8.GetString(bytes.ToArray())); }
            catch (ArgumentException) { return; }
            if (message == null) return;
            if (hosting)
            {
                // Never trust identity from payloads; Fusion supplies the sender.
                if (!session.Room.Players.Any(p => p.SessionPlayerId == Id(sender))) return;
                if (message.kind == 1)
                {
                    if (readySequences.TryGetValue(sender, out int previous) && sequence <= previous) return;
                    readySequences[sender] = sequence;
                    session.Room.TrySetReady(Id(sender), message.ready);
                }
                else if (message.kind != 0) return;
                Publish();
            }
            else if (message.kind == 2) Apply(message); // Client/server reliable traffic comes from server.
        }
        private void Publish()
        {
            var message = new Message
            {
                kind = 2, revision = ++revision, started = session.Room.IsMatchRunning,
                seats = session.Room.Players.Select(p => new Seat
                {
                    id = p.SessionPlayerId, slot = p.SlotId, name = p.DisplayName,
                    color = p.CarColorName, host = p.IsHost, ready = p.IsReady,
                    connected = p.ConnectionState == NetworkPlayerConnectionState.Connected
                }).ToArray()
            };
            Apply(message);
            foreach (PlayerRef player in runner.ActivePlayers)
                if (player != runner.LocalPlayer) Send(message, player);
        }
        private void Apply(Message message)
        {
            if (message.revision <= receivedRevision || message.seats == null || message.seats.Length > 4) return;
            receivedRevision = message.revision;
            view.LocalSessionPlayerId = Id(runner.LocalPlayer);
            view.RoomCode = code;
            view.Players = message.seats.Select(s =>
            {
                var player = new NetworkPlayer(s.id, s.slot, s.name, s.color, s.host);
                player.SetReady(s.ready);
                player.SetConnectionState(s.connected ? NetworkPlayerConnectionState.Connected : NetworkPlayerConnectionState.Disconnected);
                return player;
            }).ToArray();
            view.Phase = message.started ? NetworkGamePhase.MatchStarted : NetworkGamePhase.Lobby;
            view.CanHostStart = hosting && session.Room.CanHostStart(view.LocalSessionPlayerId);
            Report(message.started ? "Match started. Milestone 1 ends here; gameplay networking is not implemented." : "Connected. All four players must be ready.");
        }
        public static string ErrorMessage(ShutdownReason reason)
        {
            switch (reason)
            {
                case ShutdownReason.GameNotFound: return "Room not found. Check the code and use the same Photon region as the host.";
                case ShutdownReason.GameIsFull: return "Room full. Only four active players can join.";
                case ShutdownReason.GameClosed: return "Room closed. The match has started or the host is still creating it.";
                case ShutdownReason.GameIdAlreadyExists:
                case ShutdownReason.ServerInRoom: return "Room code already exists. Create again for a new code.";
                case ShutdownReason.ConnectionRefused: return "Join rejected. The room is full or the match has started.";
                default: return $"Connection ended ({reason}). Check internet and Photon settings, then retry.";
            }
        }
        private void Report(string message) { view.StatusMessage = message; ViewChanged?.Invoke(); }
        public async void LeaveRoom()
        {
            if (busy) return;
            busy = view.IsConnecting = true;
            await Cleanup();
            busy = view.IsConnecting = false;
            Report(string.Empty);
        }
        private async void LeaveAfterFailure()
        {
            if (busy) return;
            busy = view.IsConnecting = true;
            await Cleanup();
            busy = view.IsConnecting = false;
            if (!destroying) ViewChanged?.Invoke();
        }
        private async Task Cleanup()
        {
            var oldRunner = runner;
            runner = null;
            if (oldRunner != null)
            {
                oldRunner.RemoveCallbacks(callbacks);
                await oldRunner.Shutdown();
            }
            if (client != null) { client.Disconnect(); client = null; }
            view.Phase = NetworkGamePhase.Home;
            view.Players = Array.Empty<NetworkPlayer>();
            view.RoomCode = string.Empty;
            view.LocalSessionPlayerId = string.Empty;
            view.CanHostStart = false;
        }
        private void OnDestroy()
        {
            destroying = true;
            lifetime.Cancel();
            if (!busy) LeaveAfterFailure();
        }
    }
}
