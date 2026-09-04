using System.Collections.Generic;
using CrossDrive.Gameplay;

namespace CrossDrive.Scoring
{
    public enum ScoringMode { Commission = 1, Siphon = 2, SplitPurse = 3 }
    public enum ScoringPhase { Anonymous, Reveal, Known }

    public struct GemCollectedEvent
    {
        public GemCollectedEvent(int gemId, int carId, int driverId, int ownerId)
        {
            GemId = gemId; CarId = carId; DriverId = driverId; OwnerId = ownerId;
        }
        public readonly int GemId;
        public readonly int CarId;
        public readonly int DriverId;
        public readonly int OwnerId;
    }

    public struct CarEnteredPitEvent
    {
        public CarEnteredPitEvent(int carId, int ownerId, int driverId, int lastContactCarId, float lastContactAgeSeconds)
        {
            CarId = carId; OwnerId = ownerId; DriverId = driverId;
            LastContactCarId = lastContactCarId; LastContactAgeSeconds = lastContactAgeSeconds;
        }
        public readonly int CarId;
        public readonly int OwnerId;
        public readonly int DriverId;
        public readonly int LastContactCarId;
        public readonly float LastContactAgeSeconds;
    }

    public struct CarBumpEvent
    {
        public CarBumpEvent(int carA, int carB, float impulse)
        {
            CarA = carA; CarB = carB; Impulse = impulse;
        }
        public readonly int CarA;
        public readonly int CarB;
        public readonly float Impulse;
    }

    public struct CarRespawnedEvent
    {
        public CarRespawnedEvent(int carId) { CarId = carId; }
        public readonly int CarId;
    }

    public struct ScoreDelta
    {
        public ScoreDelta(int playerId, int amount) { PlayerId = playerId; Amount = amount; }
        public readonly int PlayerId;
        public readonly int Amount;
    }

    public interface IScoringStrategy
    {
        ScoringMode Mode { get; }
        string DisplayName { get; }
        string RuleSummary { get; }
        void Reset(int playerCount);
        void OnRoundStarted(ControlAssignment assignment);
        void OnGemCollected(GemCollectedEvent collectedEvent);
        void OnCarEnteredPit(CarEnteredPitEvent pitEvent);
        void OnCarBump(CarBumpEvent bumpEvent);
        void OnCarRespawned(CarRespawnedEvent respawnedEvent);
        void OnPhaseChanged(ScoringPhase phase);
        void OnTick(float secondsRemaining);
        void OnRoundEnded();
        IReadOnlyList<int> GetScores();
        string GetPlayerFeedback(int playerId);
    }
}
