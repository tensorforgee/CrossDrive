using CrossDrive.Scoring;
using UnityEngine;

namespace CrossDrive.Gameplay
{
    [RequireComponent(typeof(CarController))]
    public sealed class CarContactTracker : MonoBehaviour
    {
        private CarController car;
        private ScoringCoordinator scoring;
        private RoundTelemetryRecorder telemetry;
        private float lastContactAt = float.NegativeInfinity;

        public int LastContactCarId { get; private set; } = -1;

        public void Initialize(CarController controller, ScoringCoordinator scoringCoordinator, RoundTelemetryRecorder recorder)
        {
            car = controller;
            scoring = scoringCoordinator;
            telemetry = recorder;
            Clear();
        }

        public float GetLastContactAge()
        {
            return LastContactCarId < 0 ? -1f : Time.time - lastContactAt;
        }

        public void Clear()
        {
            LastContactCarId = -1;
            lastContactAt = float.NegativeInfinity;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            CarController other = collision.gameObject.GetComponent<CarController>();
            if (other == null) return;
            Remember(other.OwnerPlayerIndex);

            if (car.OwnerPlayerIndex < other.OwnerPlayerIndex)
            {
                CarBumpEvent bump = new CarBumpEvent(car.OwnerPlayerIndex, other.OwnerPlayerIndex, collision.relativeVelocity.magnitude);
                telemetry.RecordBump(bump, scoring.NotifyCarBump(bump));
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            CarController other = collision.gameObject.GetComponent<CarController>();
            if (other != null) Remember(other.OwnerPlayerIndex);
        }

        private void Remember(int otherCarId)
        {
            LastContactCarId = otherCarId;
            lastContactAt = Time.time;
        }
    }
}
