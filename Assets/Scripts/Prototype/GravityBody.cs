using UnityEngine;

namespace FlyMeToTheMoon
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class GravityBody : MonoBehaviour
    {
        public string planetId = "Planet";
        [Min(0.1f)] public float radius = 6f;
        [Min(0.1f)] public float captureHeight = 1.1f;
        [Min(0.1f)] public float releaseHeight = 1.5f;
        [Min(0f)] public float gravity = 16f;

        public Vector2 Center => transform.position;
        public float SurfaceDistance(Vector2 position) => Vector2.Distance(position, Center) - radius;
        public Vector2 UpAt(Vector2 position) => (position - Center).normalized;

        private void Awake() => GetComponent<CircleCollider2D>().radius = radius;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius + captureHeight);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius + releaseHeight);
        }
    }
}
