using UnityEngine;

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

        private void Start() => ResetPlayer(false);

        private void Update()
        {
            airborneSeconds = player.IsGrounded ? 0f : airborneSeconds + Time.deltaTime;
            if (gravityManager.NearestSurfaceDistance(player.Body.position) > lostDistance
                || airborneSeconds > maxAirborneSeconds)
                ResetPlayer(true);
        }

        public void ResetPlayer(bool countFailure = true)
        {
            if (countFailure) RespawnCount++;
            airborneSeconds = 0f;
            player.Respawn(respawnPlanet, spawnUp);
            cameraController.SnapToPlayer();
        }
    }
}
