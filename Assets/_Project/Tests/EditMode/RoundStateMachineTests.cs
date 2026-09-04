using CrossDrive.Gameplay;
using NUnit.Framework;

namespace CrossDrive.Tests
{
    public sealed class RoundStateMachineTests
    {
        private static RoundDurations Durations => new RoundDurations(4f, 40f, 4f, 35f, 8f);

        [Test]
        public void RoundUsesDocumentedPhaseTimingAndOrder()
        {
            RoundStateMachine round = new RoundStateMachine(Durations);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.PreRoundAssignment));
            Assert.That(float.IsPositiveInfinity(round.TimeRemaining), Is.True);

            round.CompleteAssignments();
            Assert.That(round.TimeRemaining, Is.EqualTo(4f));
            round.Tick(4f);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.AnonymousFirstHalf));
            Assert.That(round.TimeRemaining, Is.EqualTo(40f));

            round.Tick(40f);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.HalftimeReveal));
            Assert.That(round.TimeRemaining, Is.EqualTo(4f));

            round.Tick(4f);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.KnownDriverSecondHalf));
            Assert.That(round.TimeRemaining, Is.EqualTo(35f));

            round.Tick(35f);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.RoundEnd));
            Assert.That(round.TimeRemaining, Is.EqualTo(8f));

            round.Tick(8f);
            Assert.That(round.IsComplete, Is.True);
        }

        [Test]
        public void TimingCarriesOverflowAcrossPhases()
        {
            RoundStateMachine round = new RoundStateMachine(Durations);
            round.CompleteAssignments();
            round.Tick(5.5f);

            Assert.That(round.Phase, Is.EqualTo(RoundPhase.AnonymousFirstHalf));
            Assert.That(round.TimeRemaining, Is.EqualTo(38.5f).Within(0.001f));
        }

        [Test]
        public void MappingIsPreservedAcrossReveal()
        {
            ControlAssignment assignment = new ControlAssignment(new[] { 1, 2, 3, 0 });
            int[] before = assignment.CopyDriverToCarOwnerMap();
            RoundStateMachine round = new RoundStateMachine(Durations);
            round.CompleteAssignments();
            round.Tick(4f + 40f + 4f);

            Assert.That(round.Phase, Is.EqualTo(RoundPhase.KnownDriverSecondHalf));
            Assert.That(assignment.CopyDriverToCarOwnerMap(), Is.EqualTo(before));
        }
    }
}
