using System;
using System.Collections.Generic;
using CrossDrive.Gameplay;

namespace CrossDrive.Scoring
{
    public sealed class ScoringCoordinator
    {
        private IScoringStrategy strategy;
        public ScoringCoordinator(ScoringMode initialMode) { strategy = CreateStrategy(initialMode); }
        public IScoringStrategy Strategy { get { return strategy; } }
        public ScoringMode Mode { get { return strategy.Mode; } }

        public bool TrySelectMode(ScoringMode mode, RoundPhase phase, int playerCount)
        {
            if (phase != RoundPhase.PreRoundAssignment && phase != RoundPhase.RoundEnd) return false;
            if (mode == strategy.Mode) return true;
            strategy = CreateStrategy(mode);
            strategy.Reset(playerCount);
            return true;
        }

        public void Reset(int playerCount) { strategy.Reset(playerCount); }
        public IReadOnlyList<ScoreDelta> NotifyRoundStarted(ControlAssignment v) { return Capture(() => strategy.OnRoundStarted(v)); }
        public IReadOnlyList<ScoreDelta> NotifyGemCollected(GemCollectedEvent v) { return Capture(() => strategy.OnGemCollected(v)); }
        public IReadOnlyList<ScoreDelta> NotifyCarEnteredPit(CarEnteredPitEvent v) { return Capture(() => strategy.OnCarEnteredPit(v)); }
        public IReadOnlyList<ScoreDelta> NotifyCarBump(CarBumpEvent v) { return Capture(() => strategy.OnCarBump(v)); }
        public IReadOnlyList<ScoreDelta> NotifyCarRespawned(CarRespawnedEvent v) { return Capture(() => strategy.OnCarRespawned(v)); }
        public IReadOnlyList<ScoreDelta> NotifyPhaseChanged(ScoringPhase v) { return Capture(() => strategy.OnPhaseChanged(v)); }
        public void NotifyTick(float v) { strategy.OnTick(v); }
        public IReadOnlyList<ScoreDelta> NotifyRoundEnded() { return Capture(strategy.OnRoundEnded); }

        private IReadOnlyList<ScoreDelta> Capture(Action action)
        {
            IReadOnlyList<int> before = strategy.GetScores();
            int[] snapshot = new int[before.Count];
            for (int i = 0; i < before.Count; i++) snapshot[i] = before[i];
            action();
            IReadOnlyList<int> after = strategy.GetScores();
            List<ScoreDelta> deltas = new List<ScoreDelta>();
            for (int i = 0; i < after.Count; i++)
            {
                int amount = after[i] - snapshot[i];
                if (amount != 0) deltas.Add(new ScoreDelta(i, amount));
            }
            return deltas;
        }

        private static IScoringStrategy CreateStrategy(ScoringMode mode)
        {
            switch (mode)
            {
                case ScoringMode.Commission: return new CommissionScoringStrategy();
                case ScoringMode.Siphon: return new SiphonScoringStrategy();
                case ScoringMode.SplitPurse: return new SplitPurseScoringStrategy();
                default: throw new ArgumentOutOfRangeException("mode");
            }
        }
    }
}
