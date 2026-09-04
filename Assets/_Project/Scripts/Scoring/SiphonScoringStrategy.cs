using CrossDrive.Core;

namespace CrossDrive.Scoring
{
    public sealed class SiphonScoringStrategy : ScoringStrategyBase
    {
        private int[] ownerLosses = new int[0];
        public override ScoringMode Mode { get { return ScoringMode.Siphon; } }
        public override string DisplayName { get { return "SIPHON"; } }
        public override string RuleSummary { get { return "Gem: driver +2, owner -1. Pit: owner -1."; } }

        public override void Reset(int playerCount)
        {
            base.Reset(playerCount);
            ownerLosses = new int[playerCount];
        }

        public override void OnGemCollected(GemCollectedEvent value)
        {
            AddScore(value.DriverId, Phase1PrototypeConfig.SiphonGemDriverPoints);
            AddScore(value.OwnerId, Phase1PrototypeConfig.SiphonGemOwnerPoints);
            ownerLosses[value.OwnerId] += -Phase1PrototypeConfig.SiphonGemOwnerPoints;
        }

        public override void OnCarEnteredPit(CarEnteredPitEvent value)
        {
            AddScore(value.OwnerId, Phase1PrototypeConfig.SiphonPitOwnerPoints);
            ownerLosses[value.OwnerId] += -Phase1PrototypeConfig.SiphonPitOwnerPoints;
        }

        public override string GetPlayerFeedback(int playerId)
        {
            return string.Format("YOUR CAR COST YOU -{0}", ownerLosses[playerId]);
        }
    }
}
