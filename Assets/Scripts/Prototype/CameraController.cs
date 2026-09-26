using UnityEngine;

namespace FlyMeToTheMoon
{
    public sealed class CameraController : MonoBehaviour
    {
        public Transform player;
        public void SnapToPlayer()
        {
            if (player != null) transform.position = new Vector3(player.position.x, player.position.y, -10f);
        }
        // Rigidbody interpolation supplies smooth motion while keeping the player centered.
        private void LateUpdate() => SnapToPlayer();
    }
}
