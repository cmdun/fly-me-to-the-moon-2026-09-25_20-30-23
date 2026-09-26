using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlyMeToTheMoon.Instruments
{
    [DisallowMultipleComponent, RequireComponent(typeof(PianoAudioPlayer))]
    public sealed class PianoCollection : MonoBehaviour
    {
        private readonly HashSet<string> collectedIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<PianoPieceDefinition> pieces = new List<PianoPieceDefinition>();
        private PianoAudioPlayer audioPlayer;

        public int Count => pieces.Count;
        public IReadOnlyList<PianoPieceDefinition> Pieces => pieces.AsReadOnly();
        public event Action<PianoPieceDefinition> PieceCollected;
        public event Action<PianoPieceDefinition> PiecePlayed;

        private void Awake() => audioPlayer = GetComponent<PianoAudioPlayer>();

        public bool Contains(string pieceId) => pieceId != null && collectedIds.Contains(pieceId);

        public bool TryCollect(PianoPieceDefinition piece)
        {
            if (piece == null || !piece.IsValid || !collectedIds.Add(piece.pieceId)) return false;
            pieces.Add(piece);
            audioPlayer.Play(piece.sound);
            PieceCollected?.Invoke(piece);
            PiecePlayed?.Invoke(piece);
            return true;
        }

        public bool Replay(string pieceId)
        {
            if (!Contains(pieceId)) return false;
            var piece = pieces.Find(item => item.pieceId == pieceId);
            if (piece == null || piece.sound == null) return false;
            audioPlayer.Play(piece.sound);
            PiecePlayed?.Invoke(piece);
            return true;
        }
    }
}
