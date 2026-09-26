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

        public bool IsGrounded { get; private set; }
        public bool FluteUsed { get; private set; }
        public int NormalJumpCount { get; private set; }
        public int FluteJumpCount { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public bool CanFluteJump => gravityManager.IsTraveling && normalJumpUsed && !FluteUsed;
        public Vector2 Up => gravityManager.CurrentBody != null
            ? gravityManager.CurrentBody.UpAt(Body.position) : gravityManager.TravelUp;

        private InputActionAsset ownedActions;
        private InputAction moveAction;
        private InputAction jumpAction;
        private PhysicsMaterial2D frictionless;
        private float jumpRequestedUntil = -1f;
        private float ignoreGroundUntil;
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
        private void OnDisable() => ownedActions?.Disable();
        private void OnDestroy()
        {
            if (ownedActions != null) Destroy(ownedActions);
            if (frictionless != null) Destroy(frictionless);
        }

        private void Update()
        {
            if (jumpAction.WasPressedThisFrame()) jumpRequestedUntil = Time.time + 0.12f;
        }

        private void FixedUpdate()
        {
            gravityManager.UpdateLock(Body.position);
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
            }

            float move = moveAction.ReadValue<Vector2>().x;
            Vector2 velocity = Body.linearVelocity;
            float tangentSpeed = Vector2.Dot(velocity, tangent);
            float nextSpeed = Mathf.MoveTowards(tangentSpeed, move * moveSpeed,
                (IsGrounded ? groundAcceleration : airAcceleration) * Time.fixedDeltaTime);
            velocity += tangent * (nextSpeed - tangentSpeed);
            if (planet != null) velocity -= up * (planet.gravity * Time.fixedDeltaTime);

            if (jumpRequestedUntil >= Time.time)
            {
                if (IsGrounded && !normalJumpUsed)
                {
                    velocity += up * (jumpSpeed - Vector2.Dot(velocity, up));
                    normalJumpUsed = true;
                    NormalJumpCount++;
                    IsGrounded = false;
                    ignoreGroundUntil = Time.time + 0.15f;
                    jumpRequestedUntil = -1f;
                }
                else if (CanFluteJump)
                {
                    velocity += (up + tangent * move * 0.5f).normalized * fluteBoost;
                    FluteUsed = true;
                    FluteJumpCount++;
                    noteBurst.Emit(up);
                    jumpRequestedUntil = -1f;
                }
            }

            Body.linearVelocity = velocity;
            Body.SetRotation(Vector2.SignedAngle(Vector2.up, up));
        }

        public void Respawn(GravityBody home, Vector2 spawnUp)
        {
            Body.position = home.Center + spawnUp.normalized * (home.radius + radius + 0.02f);
            Body.linearVelocity = Vector2.zero;
            Body.angularVelocity = 0f;
            Body.rotation = Vector2.SignedAngle(Vector2.up, spawnUp);
            gravityManager.ResetTo(home, Body.position);
            normalJumpUsed = false;
            FluteUsed = false;
            IsGrounded = false;
            ignoreGroundUntil = Time.time + 0.05f;
            jumpRequestedUntil = -1f;
        }
    }
}
