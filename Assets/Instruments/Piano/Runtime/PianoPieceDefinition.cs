using UnityEngine;

namespace FlyMeToTheMoon.Instruments
{
    [CreateAssetMenu(menuName = "Fly Me to the Moon/Piano Piece", fileName = "PianoPiece")]
    public sealed class PianoPieceDefinition : ScriptableObject
    {
        [Tooltip("Unique collection ID. Reusing this ID makes placements the same piece.")]
        public string pieceId;
        public string displayName;
        [Tooltip("The sound this piece plays on collection. Replace the demo clip with your own music.")]
        public AudioClip sound;

        public bool IsValid => !string.IsNullOrWhiteSpace(pieceId) && sound != null;
    }
}
