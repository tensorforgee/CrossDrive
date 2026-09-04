using CrossDrive.Gameplay;
using CrossDrive.Scoring;
using NUnit.Framework;

namespace CrossDrive.Tests
{
    public sealed class ScoringStrategiesTests
    {
        private static readonly ControlAssignment Assignment = new ControlAssignment(new[] { 1, 2, 3, 0 });

        [Test]
        public void CommissionAppliesDocumentedGemAndPitDeltas()
        {
            CommissionScoringStrategy strategy = Start(new CommissionScoringStrategy());
            strategy.OnGemCollected(new GemCollectedEvent(0, 1, 0, 1));
            Assert.That(strategy.GetScores()[0], Is.EqualTo(1));
            Assert.That(strategy.GetScores()[1], Is.EqualTo(3));

            strategy.OnCarEnteredPit(new CarEnteredPitEvent(1, 1, 0, -1, -1f));
            Assert.That(strategy.GetScores()[0], Is.EqualTo(0));
            Assert.That(strategy.GetScores()[1], Is.EqualTo(1));
        }

        [Test]
        public void SiphonAppliesDocumentedDeltasWithoutDriverPitPenalty()
        {
            SiphonScoringStrategy strategy = Start(new SiphonScoringStrategy());
            strategy.OnGemCollected(new GemCollectedEvent(0, 1, 0, 1));
            strategy.OnCarEnteredPit(new CarEnteredPitEvent(1, 1, 0, -1, -1f));

            Assert.That(strategy.GetScores()[0], Is.EqualTo(2));
            Assert.That(strategy.GetScores()[1], Is.EqualTo(-2));
            Assert.That(strategy.GetPlayerFeedback(1), Is.EqualTo("YOUR CAR COST YOU -2"));
        }

        [Test]
        public void SplitPurseGemPaysOnlyOwner()
        {
            SplitPurseScoringStrategy strategy = Start(new SplitPurseScoringStrategy());
            strategy.OnGemCollected(new GemCollectedEvent(0, 1, 0, 1));

            Assert.That(strategy.GetScores()[0], Is.EqualTo(0));
            Assert.That(strategy.GetScores()[1], Is.EqualTo(3));
        }

        [Test]
        public void SplitPurseQualifyingShovePaysLastContactCarDriver()
        {
            SplitPurseScoringStrategy strategy = Start(new SplitPurseScoringStrategy());
            strategy.OnCarEnteredPit(new CarEnteredPitEvent(0, 0, 3, 2, 0.75f));

            Assert.That(strategy.GetScores()[0], Is.EqualTo(-2));
            Assert.That(strategy.GetScores()[1], Is.EqualTo(3), "Player 1 drives car 2 in the test cycle.");
        }

        [TestCase(-1, -1f)]
        [TestCase(0, 0.2f)]
        [TestCase(2, 1.01f)]
        public void SplitPurseUnattributedSelfOrExpiredPitPaysNoShoveReward(int contactCar, float age)
        {
            SplitPurseScoringStrategy strategy = Start(new SplitPurseScoringStrategy());
            strategy.OnCarEnteredPit(new CarEnteredPitEvent(0, 0, 3, contactCar, age));

            Assert.That(strategy.GetScores()[0], Is.EqualTo(-2));
            Assert.That(strategy.GetScores()[1], Is.EqualTo(0));
        }

        [Test]
        public void EventFactoryResolvesOwnerAndDriverFromAssignment()
        {
            GemCollectedEvent gem = ScoringEventFactory.CreateGemCollected(5, 2, Assignment);
            CarEnteredPitEvent pit = ScoringEventFactory.CreateCarEnteredPit(0, 2, 0.4f, Assignment);

            Assert.That(gem.OwnerId, Is.EqualTo(2));
            Assert.That(gem.DriverId, Is.EqualTo(1));
            Assert.That(pit.OwnerId, Is.EqualTo(0));
            Assert.That(pit.DriverId, Is.EqualTo(3));
        }

        [Test]
        public void ScoringModeCannotSwitchDuringActiveRound()
        {
            ScoringCoordinator scoring = new ScoringCoordinator(ScoringMode.Commission);
            scoring.Reset(4);

            Assert.That(scoring.TrySelectMode(ScoringMode.Siphon, RoundPhase.AnonymousFirstHalf, 4), Is.False);
            Assert.That(scoring.TrySelectMode(ScoringMode.SplitPurse, RoundPhase.HalftimeReveal, 4), Is.False);
            Assert.That(scoring.TrySelectMode(ScoringMode.Siphon, RoundPhase.KnownDriverSecondHalf, 4), Is.False);
            Assert.That(scoring.Mode, Is.EqualTo(ScoringMode.Commission));
            Assert.That(scoring.TrySelectMode(ScoringMode.Siphon, RoundPhase.RoundEnd, 4), Is.True);
        }

        [Test]
        public void NormalBumpsDoNotChangeAnyModelScore()
        {
            IScoringStrategy[] strategies =
            {
                Start(new CommissionScoringStrategy()),
                Start(new SiphonScoringStrategy()),
                Start(new SplitPurseScoringStrategy()),
            };

            foreach (IScoringStrategy strategy in strategies)
            {
                strategy.OnCarBump(new CarBumpEvent(0, 1, 4.5f));
                Assert.That(strategy.GetScores(), Is.EqualTo(new[] { 0, 0, 0, 0 }));
            }
        }

        private static T Start<T>(T strategy) where T : IScoringStrategy
        {
            strategy.Reset(4);
            strategy.OnRoundStarted(Assignment);
            return strategy;
        }
    }
}
