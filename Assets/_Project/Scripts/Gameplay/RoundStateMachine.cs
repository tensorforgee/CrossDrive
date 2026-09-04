using System;

namespace CrossDrive.Gameplay
{
    public enum RoundPhase
    {
        PreRoundAssignment,
        AnonymousFirstHalf,
        HalftimeReveal,
        KnownDriverSecondHalf,
        RoundEnd,
    }

    public struct RoundDurations
    {
        public RoundDurations(float preRound, float anonymous, float reveal, float knownDriver, float results)
        {
            if (preRound <= 0f || anonymous <= 0f || reveal <= 0f || knownDriver <= 0f || results <= 0f)
            {
                throw new ArgumentOutOfRangeException("preRound", "Every round phase must be longer than zero.");
            }

            PreRoundSeconds = preRound;
            AnonymousSeconds = anonymous;
            RevealSeconds = reveal;
            KnownDriverSeconds = knownDriver;
            ResultsSeconds = results;
        }

        public readonly float PreRoundSeconds;
        public readonly float AnonymousSeconds;
        public readonly float RevealSeconds;
        public readonly float KnownDriverSeconds;
        public readonly float ResultsSeconds;
    }

    public sealed class RoundStateMachine
    {
        private readonly RoundDurations durations;

        public RoundStateMachine(RoundDurations durations)
        {
            this.durations = durations;
            Phase = RoundPhase.PreRoundAssignment;
            TimeRemaining = float.PositiveInfinity;
        }

        public RoundPhase Phase { get; private set; }
        public float TimeRemaining { get; private set; }
        public bool AssignmentsComplete { get; private set; }
        public bool IsComplete { get; private set; }

        public void CompleteAssignments()
        {
            if (Phase != RoundPhase.PreRoundAssignment || AssignmentsComplete)
            {
                throw new InvalidOperationException("Assignments can only be completed once during pre-round.");
            }

            AssignmentsComplete = true;
            TimeRemaining = durations.PreRoundSeconds;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f) throw new ArgumentOutOfRangeException("deltaTime");
            if (IsComplete || (Phase == RoundPhase.PreRoundAssignment && !AssignmentsComplete)) return;

            float unusedTime = deltaTime;
            while (unusedTime >= TimeRemaining && !IsComplete)
            {
                unusedTime -= TimeRemaining;
                AdvancePhase();
            }

            if (!IsComplete) TimeRemaining -= unusedTime;
        }

        private void AdvancePhase()
        {
            switch (Phase)
            {
                case RoundPhase.PreRoundAssignment:
                    Enter(RoundPhase.AnonymousFirstHalf, durations.AnonymousSeconds);
                    break;
                case RoundPhase.AnonymousFirstHalf:
                    Enter(RoundPhase.HalftimeReveal, durations.RevealSeconds);
                    break;
                case RoundPhase.HalftimeReveal:
                    Enter(RoundPhase.KnownDriverSecondHalf, durations.KnownDriverSeconds);
                    break;
                case RoundPhase.KnownDriverSecondHalf:
                    Enter(RoundPhase.RoundEnd, durations.ResultsSeconds);
                    break;
                case RoundPhase.RoundEnd:
                    TimeRemaining = 0f;
                    IsComplete = true;
                    break;
                default:
                    throw new InvalidOperationException(string.Format("Unknown phase {0}.", Phase));
            }
        }

        private void Enter(RoundPhase phase, float duration)
        {
            Phase = phase;
            TimeRemaining = duration;
        }
    }
}
