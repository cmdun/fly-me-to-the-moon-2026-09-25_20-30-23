using UnityEngine;

namespace FlyMeToTheMoon
{
    public sealed class GravityManager : MonoBehaviour
    {
        public GravityBody[] bodies;
        public GravityBody CurrentBody { get; private set; }
        public Vector2 TravelUp { get; private set; } = Vector2.up;
        public bool IsTraveling => CurrentBody == null;

        private GravityBody jumpOrigin;
        private bool transferStarted;
        private bool clearedOrigin;

        public void ResetTo(GravityBody body, Vector2 position)
        {
            CurrentBody = body;
            TravelUp = body.UpAt(position);
            EndJump();
        }

        internal void BeginJump()
        {
            jumpOrigin = CurrentBody;
            transferStarted = false;
            clearedOrigin = false;
        }

        internal void BeginTransfer(Vector2 position)
        {
            transferStarted = true;
            TravelUp = jumpOrigin.UpAt(position);
            CurrentBody = null;
        }

        internal void EndJump()
        {
            jumpOrigin = null;
            transferStarted = false;
            clearedOrigin = false;
        }

        // A normal jump belongs to its launch planet, even beyond the lock boundary.
        // Only an explicit flute boost permits a transfer to another planet.
        public void UpdateLock(Vector2 position) => UpdateLock(position, Vector2.zero);

        public void UpdateLock(Vector2 position, Vector2 velocity)
        {
            if (jumpOrigin != null && !transferStarted)
            {
                CurrentBody = jumpOrigin;
                TravelUp = jumpOrigin.UpAt(position);
                return;
            }

            // Recoil can point back into the launch world before leaving its release zone.
            // Allow contact-speed recapture on return; an outward launch still cannot lock immediately.
            if (jumpOrigin != null && transferStarted && !clearedOrigin
                && jumpOrigin.SurfaceDistance(position) <= .5f
                && Vector2.Dot(velocity, jumpOrigin.UpAt(position)) <= 0f)
                clearedOrigin = true;

            if (jumpOrigin != null && jumpOrigin.SurfaceDistance(position) > jumpOrigin.releaseHeight)
                clearedOrigin = true;

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
                if (body == null || (body == jumpOrigin && transferStarted && !clearedOrigin)) continue;
                float distance = body.SurfaceDistance(position);
                if (distance <= body.captureHeight && distance < bestDistance)
                {
                    nearest = body;
                    bestDistance = distance;
                }
            }
            CurrentBody = nearest;
        }

        internal Vector2 TravelAcceleration(Vector2 position)
        {
            Vector2 acceleration = Vector2.zero;
            float maxGravity = 0f;
            foreach (var body in bodies)
            {
                if (body == null) continue;
                Vector2 offset = body.Center - position;
                // Finite at the center; larger worlds attract more strongly at equal distance.
                float radiusSquared = body.radius * body.radius;
                float strength = body.gravity * radiusSquared / Mathf.Max(radiusSquared, offset.sqrMagnitude);
                acceleration += offset.normalized * strength;
                maxGravity = Mathf.Max(maxGravity, body.gravity);
            }
            return Vector2.ClampMagnitude(acceleration, maxGravity);
        }

        internal Vector2 FlightUp(Vector2 position, Vector2 velocity)
        {
            if (CurrentBody != null) return CurrentBody.UpAt(position);

            GravityBody destination = null;
            float soonest = float.PositiveInfinity;
            if (velocity.sqrMagnitude > 0.01f)
            {
                foreach (var body in bodies)
                {
                    if (body == null || (body == jumpOrigin && !clearedOrigin)) continue;
                    Vector2 offset = body.Center - position;
                    float time = Vector2.Dot(offset, velocity) / velocity.sqrMagnitude;
                    if (time < 0f || time >= soonest) continue;
                    float missDistance = (offset - velocity * time).magnitude;
                    if (missDistance > body.radius + body.captureHeight) continue;
                    destination = body;
                    soonest = time;
                }
            }

            // Anticipate landing before capture instead of snapping upside down at its boundary.
            if (destination != null) return destination.UpAt(position);
            Vector2 acceleration = TravelAcceleration(position);
            return acceleration.sqrMagnitude > 0.001f ? -acceleration.normalized : TravelUp;
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
