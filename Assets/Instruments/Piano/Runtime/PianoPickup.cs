using UnityEngine;

namespace FlyMeToTheMoon.Instruments
{
    [DisallowMultipleComponent, RequireComponent(typeof(CircleCollider2D))]
    public sealed class PianoPickup : MonoBehaviour
    {
        public PianoPieceDefinition piece;

        private void Reset()
        {
            var trigger = GetComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.55f;
        }

        private void Awake() => GetComponent<CircleCollider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (piece == null || !piece.IsValid) return;
            var collection = other.GetComponentInParent<PianoCollection>();
            if (collection == null || !collection.isActiveAndEnabled) return;

            // Duplicate placements disappear without awarding or sounding the piece twice.
            collection.TryCollect(piece);
            if (collection.Contains(piece.pieceId)) gameObject.SetActive(false);
        }
    }
}
