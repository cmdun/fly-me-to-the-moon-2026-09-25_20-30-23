using UnityEngine;
using UnityEngine.InputSystem;

namespace FlyMeToTheMoon
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        public GravityManager gravityManager;
        public InputActionAsset inputActions;
        public NoteBurst noteBurst;
        public float moveSpeed = 4.5f;
        public float groundAcceleration = 35f;
        public float airAcceleration = 12f;
        public float jumpSpeed = 9f;
        public float fluteBoost = 7f;
        [Min(1f)] public float maxFlightSpeed = 18f;
        [Min(1f)] public float airRotationSpeed = 540f;

        public bool IsGrounded { get; private set; }
        public bool FluteUsed { get; private set; }
        public int NormalJumpCount { get; private set; }
        public int FluteJumpCount { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public bool IsPlayingShot => Time.time < shotUntil;
        public bool CanFluteJump => normalJumpUsed && !IsGrounded && !FluteUsed;
        public Vector2 Up => gravityManager.CurrentBody != null
            ? gravityManager.CurrentBody.UpAt(Body.position) : gravityManager.TravelUp;

        private InputActionAsset ownedActions;
        private InputAction moveAction;
        private InputAction jumpAction;
        private PhysicsMaterial2D frictionless;
        private float jumpRequestedUntil = -1f;
        private float ignoreGroundUntil;
        private float shotUntil;
        private bool normalJumpUsed;
        private float radius;

        private void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Body.gravityScale = 0f;
            Body.freezeRotation = true;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            radius = GetComponent<CircleCollider2D>().radius;
            frictionless = new PhysicsMaterial2D("Prototype player") { friction = 0f, bounciness = 0f };
            GetComponent<CircleCollider2D>().sharedMaterial = frictionless;
            // Own a copy, so enabling/disabling this player never affects other input consumers.
            ownedActions = Instantiate(inputActions);
            moveAction = ownedActions.FindAction("Player/Move", true);
            jumpAction = ownedActions.FindAction("Player/Jump", true);
        }

        private void OnEnable() => ownedActions.FindActionMap("Player", true).Enable();
        private void OnDisable()
        {
            ownedActions?.Disable();
            jumpRequestedUntil = -1f;
        }
        private void OnDestroy()
        {
            if (ownedActions != null) Destroy(ownedActions);
            if (frictionless != null) Destroy(frictionless);
        }

        private void Update()
        {
            if (!IsPlayingShot && jumpAction.WasPressedThisFrame()) jumpRequestedUntil = Time.time + 0.12f;
        }

        private void FixedUpdate()
        {
            gravityManager.UpdateLock(Body.position, Body.linearVelocity);
            Vector2 up = Up;
            Vector2 tangent = new Vector2(up.y, -up.x);
            var planet = gravityManager.CurrentBody;
            IsGrounded = planet != null && Time.time >= ignoreGroundUntil
                && planet.SurfaceDistance(Body.position) <= radius + 0.06f
                && Vector2.Dot(Body.linearVelocity, up) <= 0.5f;
            if (IsGrounded)
            {
                normalJumpUsed = false;
                FluteUsed = false;
                gravityManager.EndJump();
            }

            // Keep grounded movement responsive during a shot. Airborne shots still suppress
            // steering so firing cannot become a second movement system in flight.
            Vector2 input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
            float move = input.x;
            Vector2 velocity = Body.linearVelocity;
            if (IsPlayingShot && !IsGrounded)
            {
                // A shot suppresses steering, but never suspends gravity or cancels a transfer.
            }
            else if (FluteUsed && !IsGrounded)
            {
                // World-space WASD thrust remains stable while the visual rotates.
                // No input means coast, rather than automatically braking sideways momentum.
                velocity += input * (airAcceleration * Time.fixedDeltaTime);
            }
            else
            {
                float tangentSpeed = Vector2.Dot(velocity, tangent);
                float nextSpeed = Mathf.MoveTowards(tangentSpeed, move * moveSpeed,
                    (IsGrounded ? groundAcceleration : airAcceleration) * Time.fixedDeltaTime);
                velocity += tangent * (nextSpeed - tangentSpeed);
            }
            velocity += planet != null
                ? -up * (planet.gravity * Time.fixedDeltaTime)
                : gravityManager.TravelAcceleration(Body.position) * Time.fixedDeltaTime;

            if (!IsPlayingShot && jumpRequestedUntil >= Time.time)
            {
                if (IsGrounded && !normalJumpUsed)
                {
                    velocity += up * (jumpSpeed - Vector2.Dot(velocity, up));
                    normalJumpUsed = true;
                    gravityManager.BeginJump();
                    NormalJumpCount++;
                    IsGrounded = false;
                    ignoreGroundUntil = Time.time + 0.15f;
                    jumpRequestedUntil = -1f;
                }
                else if (CanFluteJump)
                {
                    velocity = ApplyBoost(velocity, up, tangent * move * fluteBoost * 0.5f);
                    jumpRequestedUntil = -1f;
                }
            }

            if (IsGrounded)
            {
                // Integrate an arc while attached. Tangent-only motion leaves small moons
                // when gravity cannot supply v²/r, even though the player never jumped.
                float orbitRadius = planet.radius + radius + .01f;
                float angle = Vector2.Dot(velocity, tangent) * Time.fixedDeltaTime / orbitRadius;
                Vector2 nextUp = up * Mathf.Cos(angle) + tangent * Mathf.Sin(angle);
                // Correct contact penetration as position, never as an outward impulse.
                // An impulse here would briefly detach the player after every landing.
                Vector2 surfacePosition = planet.Center + up * orbitRadius;
                Body.position = surfacePosition;
                Vector2 nextPosition = planet.Center + nextUp * orbitRadius;
                velocity = (nextPosition - surfacePosition) / Time.fixedDeltaTime;
            }
            Body.linearVelocity = FluteUsed ? Vector2.ClampMagnitude(velocity, maxFlightSpeed) : velocity;
            Vector2 facingUp = FluteUsed ? gravityManager.FlightUp(Body.position, Body.linearVelocity) : up;
            float targetAngle = Vector2.SignedAngle(Vector2.up, facingUp);
            Body.SetRotation(IsGrounded ? targetAngle : Mathf.MoveTowardsAngle(
                Body.rotation, targetAngle, airRotationSpeed * Time.fixedDeltaTime));
        }

        public void BeginShot(float duration = .24f)
        {
            shotUntil = Time.time + duration;
            jumpRequestedUntil = -1f;
        }

        public bool TryDrumLaunch(float speed)
        {
            if (!enabled || !IsGrounded || IsPlayingShot) return false;
            Vector2 up = Up;
            Body.linearVelocity += up * (speed - Vector2.Dot(Body.linearVelocity, up));
            normalJumpUsed = true; FluteUsed = false; IsGrounded = false;
            NormalJumpCount++; gravityManager.BeginJump();
            ignoreGroundUntil = Time.time + .18f; jumpRequestedUntil = -1f;
            return true;
        }

        // Mouse recoil and Space share one airborne boost budget.
        public bool TryDirectionalBoost(Vector2 direction)
        {
            if (!enabled || !CanFluteJump || direction.sqrMagnitude < 0.001f) return false;
            Body.linearVelocity = ApplyBoost(Body.linearVelocity, direction.normalized, Vector2.zero);
            jumpRequestedUntil = -1f;
            return true;
        }

        private Vector2 ApplyBoost(Vector2 velocity, Vector2 direction, Vector2 steering)
        {
            float projected = Vector2.Dot(velocity, direction);
            velocity += direction * (Mathf.Max(jumpSpeed, projected + fluteBoost) - projected);
            velocity += steering;
            gravityManager.BeginTransfer(Body.position);
            FluteUsed = true;
            FluteJumpCount++;
            if (noteBurst != null) noteBurst.Emit(direction);
            return Vector2.ClampMagnitude(velocity, maxFlightSpeed);
        }

        public void Respawn(GravityBody home, Vector2 spawnUp)
        {
            Body.position = home.Center + spawnUp.normalized * (home.radius + radius + 0.02f);
            Body.linearVelocity = Vector2.zero;
            Body.angularVelocity = 0f;
            Body.rotation = Vector2.SignedAngle(Vector2.up, spawnUp);
            gravityManager.ResetTo(home, Body.position);
            normalJumpUsed = false;
            shotUntil = 0;
            FluteUsed = false;
            IsGrounded = false;
            ignoreGroundUntil = Time.time + 0.05f;
            jumpRequestedUntil = -1f;
        }
    }
}
