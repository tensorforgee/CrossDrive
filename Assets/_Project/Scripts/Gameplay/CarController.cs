using CrossDrive.Input;
using UnityEngine;

namespace CrossDrive.Gameplay
{
    public sealed class CarController : MonoBehaviour
    {
        private LocalKeyboardInput keyboardInput;
        private Collider2D carCollider;
        private bool roundDrivingEnabled;

        public int OwnerPlayerIndex { get; private set; }
        public int DriverPlayerIndex { get; private set; }
        public CarMovement Movement { get; private set; }
        public bool IsRespawning { get; private set; }
        public Vector3 RestingScale { get; private set; }

        public void Initialize(int ownerPlayerIndex, CarMovement movement, LocalKeyboardInput input)
        {
            OwnerPlayerIndex = ownerPlayerIndex;
            DriverPlayerIndex = -1;
            Movement = movement;
            keyboardInput = input;
            carCollider = GetComponent<Collider2D>();
            RestingScale = transform.localScale;
        }

        public void AssignDriver(int driverPlayerIndex)
        {
            DriverPlayerIndex = driverPlayerIndex;
        }

        public void SetRoundDrivingEnabled(bool enabled)
        {
            roundDrivingEnabled = enabled;
            Movement.SetCanDrive(roundDrivingEnabled && !IsRespawning);
        }

        public void SetRespawning(bool respawning)
        {
            IsRespawning = respawning;
            carCollider.enabled = !respawning;
            Movement.SetCanDrive(roundDrivingEnabled && !respawning);
        }

        public void SetFallVisualScale(float scale) => transform.localScale = RestingScale * scale;

        private void Update()
        {
            if (!Movement.CanDrive || DriverPlayerIndex < 0)
            {
                return;
            }

            Movement.SubmitInput(
                keyboardInput.ReadSteering(DriverPlayerIndex),
                keyboardInput.ReadBoostDown(DriverPlayerIndex));
        }
    }
}
