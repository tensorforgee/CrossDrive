using System.Collections;
using CrossDrive.Core;
using CrossDrive.Scoring;
using UnityEngine;

namespace CrossDrive.Gameplay
{
    public sealed class RespawnSystem : MonoBehaviour
    {
        private const float FallAnimationSeconds = 0.25f;
        private ArenaManager arena;
        private CarController[] cars;
        private ScoringCoordinator scoring;
        private RoundTelemetryRecorder telemetry;
        private RoundManager round;
        private bool roundDrivingEnabled;

        public void Initialize(ArenaManager arenaManager, CarController[] carControllers,
            ScoringCoordinator scoringCoordinator, RoundTelemetryRecorder recorder)
        {
            arena = arenaManager;
            cars = carControllers;
            scoring = scoringCoordinator;
            telemetry = recorder;
        }

        public void BindRound(RoundManager roundManager) => round = roundManager;

        public void SetRoundDrivingEnabled(bool enabled)
        {
            roundDrivingEnabled = enabled;
            foreach (CarController car in cars) car.SetRoundDrivingEnabled(enabled);
        }

        public void ResetAllCars()
        {
            StopAllCoroutines();
            for (int owner = 0; owner < cars.Length; owner++)
            {
                CarController car = cars[owner];
                car.SetFallVisualScale(1f);
                car.SetRespawning(false);
                car.GetComponent<CarContactTracker>().Clear();
                car.Movement.ResetMotion(arena.GetSpawnPoint(owner), arena.GetSpawnRotation(owner));
                car.SetRoundDrivingEnabled(roundDrivingEnabled);
            }
        }

        private void Update()
        {
            if (!roundDrivingEnabled || round == null) return;
            foreach (CarController car in cars)
            {
                if (!car.IsRespawning && !arena.IsDriveable(car.transform.position)) StartCoroutine(Respawn(car));
            }
        }

        private IEnumerator Respawn(CarController car)
        {
            car.SetRespawning(true);
            CarContactTracker contact = car.GetComponent<CarContactTracker>();
            CarEnteredPitEvent pit = ScoringEventFactory.CreateCarEnteredPit(
                car.OwnerPlayerIndex, contact.LastContactCarId, contact.GetLastContactAge(), round.Assignment);
            telemetry.RecordPit(pit, scoring.NotifyCarEnteredPit(pit));

            float elapsed = 0f;
            while (elapsed < FallAnimationSeconds)
            {
                elapsed += Time.deltaTime;
                car.SetFallVisualScale(1f - Mathf.Clamp01(elapsed / FallAnimationSeconds));
                yield return null;
            }

            yield return new WaitForSeconds(Phase1PrototypeConfig.CarRespawnSeconds - FallAnimationSeconds);
            car.Movement.ResetMotion(arena.GetSpawnPoint(car.OwnerPlayerIndex), arena.GetSpawnRotation(car.OwnerPlayerIndex));
            car.SetFallVisualScale(1f);
            contact.Clear();
            car.SetRespawning(false);
            car.SetRoundDrivingEnabled(roundDrivingEnabled);

            CarRespawnedEvent respawned = new CarRespawnedEvent(car.OwnerPlayerIndex);
            telemetry.RecordRespawn(respawned, scoring.NotifyCarRespawned(respawned));
        }
    }
}
