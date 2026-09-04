using CrossDrive.Core;

namespace CrossDrive.Scoring
{
    public sealed class CommissionScoringStrategy : ScoringStrategyBase
    {
        public override ScoringMode Mode { get { return ScoringMode.Commission; } }
        public override string DisplayName { get { return "COMMISSION"; } }
        public override string RuleSummary { get { return "Gem: owner +3, driver +1. Pit: owner -2, driver -1."; } }

        public override void OnGemCollected(GemCollectedEvent value)
        {
            AddScore(value.OwnerId, Phase1PrototypeConfig.CommissionGemOwnerPoints);
            AddScore(value.DriverId, Phase1PrototypeConfig.CommissionGemDriverPoints);
        }

        public override void OnCarEnteredPit(CarEnteredPitEvent value)
        {
            AddScore(value.OwnerId, Phase1PrototypeConfig.CommissionPitOwnerPoints);
            AddScore(value.DriverId, Phase1PrototypeConfig.CommissionPitDriverPoints);
        }
    }
}
