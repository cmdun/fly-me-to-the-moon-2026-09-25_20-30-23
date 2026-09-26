using UnityEngine;
using UnityEngine.InputSystem;

namespace FlyMeToTheMoon
{
    public sealed class PlanetManager : MonoBehaviour
    {
        public PlayerController player;
        public GravityManager gravityManager;
        public GravityBody respawnPlanet;
        public CameraController cameraController;
        public Vector2 spawnUp = Vector2.up;
        public float lostDistance = 22f;
        public float maxAirborneSeconds = 12f;
        public GravityBody CurrentPlanet => gravityManager.CurrentBody;
        public int RespawnCount { get; private set; }
        private float airborneSeconds;
        private GravityBody lastSafePlanet;
        private Vector2 lastSafeUp;
        private bool wasGrounded;
        private InputAction recoverAction;

        private void Awake() => recoverAction = new InputAction("Recover", InputActionType.Button, "<Keyboard>/r");
        private void OnEnable() => recoverAction.Enable();
        private void OnDisable() => recoverAction.Disable();
        private void OnDestroy() => recoverAction.Dispose();

        private void Start() => ResetPlayer(false);

        private void Update()
        {
            if (player.IsGrounded && !wasGrounded && CurrentPlanet != null)
            {
                lastSafePlanet = CurrentPlanet;
                lastSafeUp = CurrentPlanet.UpAt(player.Body.position);
            }
            wasGrounded = player.IsGrounded;
            airborneSeconds = player.IsGrounded ? 0f : airborneSeconds + Time.deltaTime;
            if (recoverAction.WasPressedThisFrame()
                || gravityManager.NearestSurfaceDistance(player.Body.position) > lostDistance
                || airborneSeconds > maxAirborneSeconds)
                ResetPlayer(true);
        }

        public void ResetPlayer(bool countFailure = true)
        {
            if (countFailure) RespawnCount++;
            airborneSeconds = 0f;
            wasGrounded = false;
            player.Respawn(lastSafePlanet != null ? lastSafePlanet : respawnPlanet,
                lastSafePlanet != null ? lastSafeUp : spawnUp);
            cameraController.SnapToPlayer();
        }
    }
}
