using System;
using System.Collections.Generic;
using System.Linq;
using CrossDrive.Core;
using CrossDrive.Gameplay;
using CrossDrive.Scoring;

namespace CrossDrive.Networking
{
    public sealed class NetworkGemState
    {
        internal NetworkGemState(int gemId) { GemId = gemId; IsVisible = true; }
        public int GemId { get; }
        public int Generation { get; internal set; }
        public bool IsVisible { get; internal set; }
        public double RespawnAt { get; internal set; }
        public float PositionX { get; internal set; }
        public float PositionY { get; internal set; }
    }

    public sealed class NetworkCarState
    {
        internal NetworkCarState(int carId) { CarId = carId; }
        public int CarId { get; }
        public int LifeId { get; internal set; }
        public bool IsRespawning { get; internal set; }
        public double RespawnAt { get; internal set; }
        public float PositionX { get; internal set; }
        public float PositionY { get; internal set; }
        public float RotationDegrees { get; internal set; }
        internal float SpawnX { get; set; }
        internal float SpawnY { get; set; }
        internal float SpawnRotationDegrees { get; set; }
    }

    public sealed class NetworkMatchSnapshot
    {
        public NetworkGamePhase Phase;
        public double PhaseEndsAt;
        public string StatusMessage = string.Empty;
        public ScoringMode ScoringMode;
        public int[] Scores = Array.Empty<int>();
        public int[] RevealedDriverToCarOwnerMap;
        public NetworkGemState[] Gems = Array.Empty<NetworkGemState>();
        public NetworkCarState[] Cars = Array.Empty<NetworkCarState>();
    }

    public sealed class NetworkRoundCoordinator
    {
        private static readonly RoundDurations Durations = new RoundDurations(
            Phase1PrototypeConfig.PreRoundSeconds,
            Phase1PrototypeConfig.AnonymousSeconds,
            Phase1PrototypeConfig.RevealSeconds,
            Phase1PrototypeConfig.KnownDriverSeconds,
            Phase1PrototypeConfig.ResultsSeconds);

        private readonly ControlAssignmentService assignmentService = new ControlAssignmentService();
        private readonly HashSet<long> processedGemPickups = new HashSet<long>();
        private readonly HashSet<long> processedPitEntries = new HashSet<long>();
        private readonly NetworkGemState[] gems = new NetworkGemState[Phase1PrototypeConfig.ActiveGemCount];
        private readonly NetworkCarState[] cars = new NetworkCarState[Phase1PrototypeConfig.PlayerCount];
        private ScoringCoordinator scoring = new ScoringCoordinator(ScoringMode.Siphon);
        private PrivateAssignmentState privateAssignments;
        private IReadOnlyList<NetworkPlayer> players = Array.Empty<NetworkPlayer>();

        public NetworkRoundCoordinator()
        {
            for (int index = 0; index < gems.Length; index++) gems[index] = new NetworkGemState(index);
            for (int index = 0; index < cars.Length; index++) cars[index] = new NetworkCarState(index);
        }

        public NetworkGamePhase Phase { get; private set; } = NetworkGamePhase.Lobby;
        public double PhaseEndsAt { get; private set; }
        public string StatusMessage { get; private set; } = string.Empty;
        public ScoringMode ScoringMode => scoring.Mode;
        public ControlAssignment Assignment => privateAssignments?.Assignment;
        public IReadOnlyList<int> Scores => scoring.Strategy.GetScores();
        public IReadOnlyList<NetworkGemState> Gems => gems;
        public IReadOnlyList<NetworkCarState> Cars => cars;
        public bool IsDrivingEnabled => Phase == NetworkGamePhase.Anonymous || Phase == NetworkGamePhase.Known;

        public bool BeginAssignment(bool hasStateAuthority, IReadOnlyList<NetworkPlayer> roomPlayers, int randomSeed,
            ScoringMode scoringMode = ScoringMode.Siphon)
        {
            if (!hasStateAuthority || roomPlayers == null || roomPlayers.Count != Phase1PrototypeConfig.PlayerCount ||
                roomPlayers.Any(player => player.ConnectionState != NetworkPlayerConnectionState.Connected)) return false;

            players = roomPlayers.OrderBy(player => player.SlotId).ToArray();
            ControlAssignment assignment = assignmentService.Create(Phase1PrototypeConfig.PlayerCount, new Random(randomSeed));
            privateAssignments = new PrivateAssignmentState(assignment, players);
            scoring = new ScoringCoordinator(scoringMode);
            scoring.Reset(Phase1PrototypeConfig.PlayerCount);
            scoring.NotifyRoundStarted(assignment);
            processedGemPickups.Clear();
            processedPitEntries.Clear();
            ResetReplicatedObjects();
            Phase = NetworkGamePhase.Assignment;
            PhaseEndsAt = 0d;
            StatusMessage = "Private assignments are being delivered.";
            return true;
        }

        public PrivateAssignmentView CreatePrivateAssignmentView(bool hasStateAuthority, string sessionPlayerId)
        {
            if (!hasStateAuthority || Phase != NetworkGamePhase.Assignment || privateAssignments == null) return null;
            return privateAssignments.CreatePrivateView(sessionPlayerId);
        }

        public bool ConfirmPrivateAssignmentsDelivered(bool hasStateAuthority, double networkTime)
        {
            if (!hasStateAuthority || Phase != NetworkGamePhase.Assignment) return false;
            Enter(NetworkGamePhase.Countdown, networkTime + Durations.PreRoundSeconds);
            StatusMessage = string.Empty;
            return true;
        }

        public bool Tick(bool hasStateAuthority, double networkTime)
        {
            if (!hasStateAuthority) return false;
            if (Phase == NetworkGamePhase.Interrupted) return false;
            bool changed = false;
            while (IsTimedPhase(Phase) && networkTime >= PhaseEndsAt)
            {
                changed = true;
                switch (Phase)
                {
                    case NetworkGamePhase.Countdown:
                        Enter(NetworkGamePhase.Anonymous, PhaseEndsAt + Durations.AnonymousSeconds);
                        scoring.NotifyPhaseChanged(ScoringPhase.Anonymous);
                        break;
                    case NetworkGamePhase.Anonymous:
                        privateAssignments.PublishReveal();
                        Enter(NetworkGamePhase.Reveal, PhaseEndsAt + Durations.RevealSeconds);
                        scoring.NotifyPhaseChanged(ScoringPhase.Reveal);
                        break;
                    case NetworkGamePhase.Reveal:
                        Enter(NetworkGamePhase.Known, PhaseEndsAt + Durations.KnownDriverSeconds);
                        scoring.NotifyPhaseChanged(ScoringPhase.Known);
                        break;
                    case NetworkGamePhase.Known:
                        scoring.NotifyRoundEnded();
                        Enter(NetworkGamePhase.Results, PhaseEndsAt + Durations.ResultsSeconds);
                        break;
                    case NetworkGamePhase.Results:
                        PhaseEndsAt = 0d;
                        break;
                }
            }

            for (int index = 0; index < gems.Length; index++)
            {
                NetworkGemState gem = gems[index];
                if (!gem.IsVisible && networkTime >= gem.RespawnAt)
                {
                    gem.Generation++;
                    gem.IsVisible = true;
                    gem.RespawnAt = 0d;
                }
            }

            for (int index = 0; index < cars.Length; index++)
            {
                NetworkCarState car = cars[index];
                if (car.IsRespawning && networkTime >= car.RespawnAt)
                {
                    car.IsRespawning = false;
                    car.RespawnAt = 0d;
                    car.LifeId++;
                    car.PositionX = car.SpawnX;
                    car.PositionY = car.SpawnY;
                    car.RotationDegrees = car.SpawnRotationDegrees;
                    scoring.NotifyCarRespawned(new CarRespawnedEvent(car.CarId));
                }
            }
            return changed;
        }

        public bool TryCollectGem(bool hasStateAuthority, int gemId, int generation, int carId, double networkTime,
            out IReadOnlyList<ScoreDelta> deltas)
        {
            deltas = Array.Empty<ScoreDelta>();
            if (!hasStateAuthority || !IsGameplayPhase() || gemId < 0 || gemId >= gems.Length ||
                carId < 0 || carId >= cars.Length || cars[carId].IsRespawning) return false;

            NetworkGemState gem = gems[gemId];
            long key = EventKey(gemId, generation);
            if (!gem.IsVisible || gem.Generation != generation || !processedGemPickups.Add(key)) return false;

            gem.IsVisible = false;
            gem.RespawnAt = networkTime + Phase1PrototypeConfig.GemRespawnSeconds;
            GemCollectedEvent collected = ScoringEventFactory.CreateGemCollected(gemId, carId, Assignment);
            deltas = scoring.NotifyGemCollected(collected);
            return true;
        }

        public bool TryEnterPit(bool hasStateAuthority, int carId, int lifeId, int lastContactCarId,
            float lastContactAgeSeconds, double networkTime, out IReadOnlyList<ScoreDelta> deltas)
        {
            deltas = Array.Empty<ScoreDelta>();
            if (!hasStateAuthority || !IsGameplayPhase() || carId < 0 || carId >= cars.Length) return false;

            NetworkCarState car = cars[carId];
            long key = EventKey(carId, lifeId);
            if (car.IsRespawning || car.LifeId != lifeId || !processedPitEntries.Add(key)) return false;

            car.IsRespawning = true;
            car.RespawnAt = networkTime + Phase1PrototypeConfig.CarRespawnSeconds;
            CarEnteredPitEvent pit = ScoringEventFactory.CreateCarEnteredPit(carId, lastContactCarId, lastContactAgeSeconds, Assignment);
            deltas = scoring.NotifyCarEnteredPit(pit);
            return true;
        }

        public bool TryApplyAuthoritativeCarState(bool hasStateAuthority, int carId, float x, float y, float rotationDegrees)
        {
            if (!hasStateAuthority || !IsDrivingEnabled || carId < 0 || carId >= cars.Length || cars[carId].IsRespawning) return false;
            cars[carId].PositionX = x;
            cars[carId].PositionY = y;
            cars[carId].RotationDegrees = rotationDegrees;
            return true;
        }

        public bool TryConfigureCarSpawn(bool hasStateAuthority, int carId, float x, float y, float rotationDegrees)
        {
            if (!hasStateAuthority || carId < 0 || carId >= cars.Length) return false;
            NetworkCarState car = cars[carId];
            car.SpawnX = x;
            car.SpawnY = y;
            car.SpawnRotationDegrees = rotationDegrees;
            if (!car.IsRespawning)
            {
                car.PositionX = x;
                car.PositionY = y;
                car.RotationDegrees = rotationDegrees;
            }
            return true;
        }

        public bool TryApplyAuthoritativeGemPosition(bool hasStateAuthority, int gemId, float x, float y)
        {
            if (!hasStateAuthority || gemId < 0 || gemId >= gems.Length) return false;
            gems[gemId].PositionX = x;
            gems[gemId].PositionY = y;
            return true;
        }

        public int ResolveControlledCar(string sessionPlayerId)
        {
            if (Assignment == null) return -1;
            NetworkPlayer player = players.FirstOrDefault(value => value.SessionPlayerId == sessionPlayerId);
            return player == null ? -1 : Assignment.GetControlledCarOwner(player.SlotId);
        }

        public void Interrupt(bool hasStateAuthority, string reason)
        {
            if (!hasStateAuthority) return;
            Phase = NetworkGamePhase.Interrupted;
            PhaseEndsAt = 0d;
            StatusMessage = string.IsNullOrWhiteSpace(reason) ? "Match ended because a player disconnected." : reason;
        }

        public NetworkMatchSnapshot CreateSnapshot()
        {
            return new NetworkMatchSnapshot
            {
                Phase = Phase,
                PhaseEndsAt = PhaseEndsAt,
                StatusMessage = StatusMessage,
                ScoringMode = ScoringMode,
                Scores = Scores.ToArray(),
                RevealedDriverToCarOwnerMap = privateAssignments?.CopyRevealedDriverToCarOwnerMap(),
                Gems = gems.Select(CopyGem).ToArray(),
                Cars = cars.Select(CopyCar).ToArray(),
            };
        }

        private void ResetReplicatedObjects()
        {
            foreach (NetworkGemState gem in gems)
            {
                gem.Generation = 0;
                gem.IsVisible = true;
                gem.RespawnAt = 0d;
                gem.PositionX = 0f;
                gem.PositionY = 0f;
            }
            foreach (NetworkCarState car in cars)
            {
                car.LifeId = 0;
                car.IsRespawning = false;
                car.RespawnAt = 0d;
                car.PositionX = 0f;
                car.PositionY = 0f;
                car.RotationDegrees = 0f;
                car.SpawnX = 0f;
                car.SpawnY = 0f;
                car.SpawnRotationDegrees = 0f;
            }
        }

        private bool IsGameplayPhase() => IsDrivingEnabled;

        private static bool IsTimedPhase(NetworkGamePhase phase)
        {
            return phase == NetworkGamePhase.Countdown || phase == NetworkGamePhase.Anonymous ||
                   phase == NetworkGamePhase.Reveal || phase == NetworkGamePhase.Known;
        }

        private void Enter(NetworkGamePhase phase, double phaseEndsAt)
        {
            Phase = phase;
            PhaseEndsAt = phaseEndsAt;
        }

        private static long EventKey(int id, int generation) => ((long)id << 32) | (uint)generation;

        private static NetworkGemState CopyGem(NetworkGemState source)
        {
            return new NetworkGemState(source.GemId)
            {
                Generation = source.Generation,
                IsVisible = source.IsVisible,
                RespawnAt = source.RespawnAt,
                PositionX = source.PositionX,
                PositionY = source.PositionY,
            };
        }

        private static NetworkCarState CopyCar(NetworkCarState source)
        {
            return new NetworkCarState(source.CarId)
            {
                LifeId = source.LifeId,
                IsRespawning = source.IsRespawning,
                RespawnAt = source.RespawnAt,
                PositionX = source.PositionX,
                PositionY = source.PositionY,
                RotationDegrees = source.RotationDegrees,
                SpawnX = source.SpawnX,
                SpawnY = source.SpawnY,
                SpawnRotationDegrees = source.SpawnRotationDegrees,
            };
        }
    }
}
