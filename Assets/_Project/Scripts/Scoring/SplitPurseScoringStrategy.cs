using CrossDrive.Core;

namespace CrossDrive.Scoring
{
    public sealed class SplitPurseScoringStrategy : ScoringStrategyBase
    {
        public override ScoringMode Mode { get { return ScoringMode.SplitPurse; } }
        public override string DisplayName { get { return "SPLIT PURSE"; } }
        public override string RuleSummary { get { return "Gem: owner +3. Pit: owner -2; qualifying shover's driver +3."; } }

        public override void OnGemCollected(GemCollectedEvent value)
        {
            AddScore(value.OwnerId, Phase1PrototypeConfig.SplitPurseGemOwnerPoints);
        }

        public override void OnCarEnteredPit(CarEnteredPitEvent value)
        {
            AddScore(value.OwnerId, Phase1PrototypeConfig.SplitPursePitOwnerPoints);
            bool qualifies = value.LastContactCarId >= 0 && value.LastContactCarId != value.CarId &&
                             value.LastContactAgeSeconds >= 0f &&
                             value.LastContactAgeSeconds <= Phase1PrototypeConfig.ShoveAttributionSeconds;
            if (qualifies)
            {
                AddScore(Assignment.GetDriverForCarOwner(value.LastContactCarId), Phase1PrototypeConfig.SplitPurseShoveDriverPoints);
            }
        }
    }
}
