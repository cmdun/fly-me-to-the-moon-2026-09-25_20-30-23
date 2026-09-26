using UnityEngine;

namespace FlyMeToTheMoon
{
    public sealed class GravityManager : MonoBehaviour
    {
        public GravityBody[] bodies;
        public GravityBody CurrentBody { get; private set; }
        public Vector2 TravelUp { get; private set; } = Vector2.up;
        public bool IsTraveling => CurrentBody == null;

        public void ResetTo(GravityBody body, Vector2 position)
        {
            CurrentBody = body;
            TravelUp = body.UpAt(position);
        }

        // Separate capture/release distances prevent rapid lock switching at a boundary.
        public void UpdateLock(Vector2 position)
        {
            if (CurrentBody != null)
            {
                TravelUp = CurrentBody.UpAt(position);
                if (CurrentBody.SurfaceDistance(position) <= CurrentBody.releaseHeight)
                    return;
                CurrentBody = null;
            }

            GravityBody nearest = null;
            float bestDistance = float.PositiveInfinity;
            foreach (var body in bodies)
            {
                if (body == null) continue;
                float distance = body.SurfaceDistance(position);
                if (distance <= body.captureHeight && distance < bestDistance)
                {
                    nearest = body;
                    bestDistance = distance;
                }
            }
            CurrentBody = nearest;
        }

        public float NearestSurfaceDistance(Vector2 position)
        {
            float nearest = float.PositiveInfinity;
            foreach (var body in bodies)
                if (body != null) nearest = Mathf.Min(nearest, body.SurfaceDistance(position));
            return nearest;
        }
    }
}
