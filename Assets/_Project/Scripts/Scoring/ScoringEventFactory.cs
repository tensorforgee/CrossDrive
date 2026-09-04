using System;
using CrossDrive.Gameplay;

namespace CrossDrive.Scoring
{
    public static class ScoringEventFactory
    {
        public static GemCollectedEvent CreateGemCollected(int gemId, int carId, ControlAssignment assignment)
        {
            ValidateCar(carId, assignment);
            return new GemCollectedEvent(gemId, carId, assignment.GetDriverForCarOwner(carId), carId);
        }

        public static CarEnteredPitEvent CreateCarEnteredPit(int carId, int lastContactCarId, float age, ControlAssignment assignment)
        {
            ValidateCar(carId, assignment);
            return new CarEnteredPitEvent(carId, carId, assignment.GetDriverForCarOwner(carId), lastContactCarId, age);
        }

        private static void ValidateCar(int carId, ControlAssignment assignment)
        {
            if (assignment == null) throw new ArgumentNullException("assignment");
            if (carId < 0 || carId >= assignment.PlayerCount) throw new ArgumentOutOfRangeException("carId");
        }
    }
}
