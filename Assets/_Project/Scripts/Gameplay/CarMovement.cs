using UnityEngine;

namespace CrossDrive.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CarMovement : MonoBehaviour
    {
        private const float AutoAcceleration = 8.5f;
        private const float SteeringDegreesPerSecond = 155f;
        private const float MaximumSpeed = 8.25f;
        private const float BoostImpulse = 5.25f;
        private const float BoostCooldownSeconds = 1.4f;
        private const float LateralVelocityRetention = 0.84f;

        private Rigidbody2D body;
        private float steering;
        private bool boostQueued;
        private float boostReadyAt;

        public bool CanDrive { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.linearDamping = 0.35f;
            body.angularDamping = 5f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        public void SubmitInput(float steeringInput, bool boostPressed)
        {
            steering = Mathf.Clamp(steeringInput, -1f, 1f);
            boostQueued |= boostPressed;
        }

        public void SetCanDrive(bool canDrive)
        {
            CanDrive = canDrive;
            if (!canDrive)
            {
                steering = 0f;
                boostQueued = false;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
        }

        public void ResetMotion(Vector2 position, float rotationDegrees)
        {
            body.position = position;
            body.rotation = rotationDegrees;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, 0f, rotationDegrees);
        }

        private void FixedUpdate()
        {
            if (!CanDrive)
            {
                return;
            }

            Vector2 forward = transform.up;
            Vector2 right = transform.right;
            Vector2 velocity = body.linearVelocity;
            Vector2 forwardVelocity = forward * Vector2.Dot(velocity, forward);
            Vector2 lateralVelocity = right * Vector2.Dot(velocity, right);
            body.linearVelocity = forwardVelocity + lateralVelocity * LateralVelocityRetention;

            body.AddForce(forward * AutoAcceleration, ForceMode2D.Force);

            float speedFactor = Mathf.Clamp01(body.linearVelocity.magnitude / 2f);
            body.MoveRotation(body.rotation + steering * SteeringDegreesPerSecond * speedFactor * Time.fixedDeltaTime);

            if (boostQueued && Time.time >= boostReadyAt)
            {
                body.AddForce(forward * BoostImpulse, ForceMode2D.Impulse);
                boostReadyAt = Time.time + BoostCooldownSeconds;
            }

            boostQueued = false;
            body.linearVelocity = Vector2.ClampMagnitude(body.linearVelocity, MaximumSpeed);
        }
    }
}
