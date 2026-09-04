using System;
using System.Collections.Generic;
using CrossDrive.Gameplay;

namespace CrossDrive.Scoring
{
    public abstract class ScoringStrategyBase : IScoringStrategy
    {
        private int[] scores = new int[0];
        protected ControlAssignment Assignment { get; private set; }
        public abstract ScoringMode Mode { get; }
        public abstract string DisplayName { get; }
        public abstract string RuleSummary { get; }

        public virtual void Reset(int playerCount)
        {
            if (playerCount <= 0) throw new ArgumentOutOfRangeException("playerCount");
            scores = new int[playerCount];
            Assignment = null;
        }

        public virtual void OnRoundStarted(ControlAssignment assignment)
        {
            if (assignment == null) throw new ArgumentNullException("assignment");
            Assignment = assignment;
        }

        public abstract void OnGemCollected(GemCollectedEvent collectedEvent);
        public abstract void OnCarEnteredPit(CarEnteredPitEvent pitEvent);
        public virtual void OnCarBump(CarBumpEvent bumpEvent) { }
        public virtual void OnCarRespawned(CarRespawnedEvent respawnedEvent) { }
        public virtual void OnPhaseChanged(ScoringPhase phase) { }
        public virtual void OnTick(float secondsRemaining) { }
        public virtual void OnRoundEnded() { }
        public IReadOnlyList<int> GetScores() { return scores; }
        public virtual string GetPlayerFeedback(int playerId) { return string.Empty; }

        protected void AddScore(int playerId, int amount)
        {
            if (playerId < 0 || playerId >= scores.Length) throw new ArgumentOutOfRangeException("playerId");
            scores[playerId] += amount;
        }
    }
}
