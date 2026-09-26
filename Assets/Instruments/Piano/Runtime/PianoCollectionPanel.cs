using UnityEngine;

namespace FlyMeToTheMoon.Instruments
{
    public sealed class PianoCollectionPanel : MonoBehaviour
    {
        public PianoCollection collection;
        public UnityEngine.UI.Text heading;
        public UnityEngine.UI.Text status;
        public UnityEngine.UI.Button[] replayButtons;
        public UnityEngine.UI.Text[] replayLabels;
        public UnityEngine.UI.Button previousButton;
        public UnityEngine.UI.Button nextButton;
        private int page;

        private void Awake()
        {
            for (int i = 0; i < replayButtons.Length; i++)
            {
                int slot = i;
                replayButtons[i].onClick.AddListener(() => ReplaySlot(slot));
            }
            previousButton.onClick.AddListener(() => { page--; Refresh(); });
            nextButton.onClick.AddListener(() => { page++; Refresh(); });
        }

        private void OnEnable()
        {
            if (collection == null) return;
            collection.PieceCollected += Collected;
            collection.PiecePlayed += Played;
            Refresh();
        }

        private void OnDisable()
        {
            if (collection == null) return;
            collection.PieceCollected -= Collected;
            collection.PiecePlayed -= Played;
        }

        private void Collected(PianoPieceDefinition piece)
        {
            page = (collection.Count - 1) / replayButtons.Length;
            Refresh();
        }

        private void Played(PianoPieceDefinition piece) => status.text = "Playing: " + piece.displayName;

        public void ReplaySlot(int slot)
        {
            if (collection == null || slot < 0 || slot >= replayButtons.Length) return;
            int index = page * replayButtons.Length + slot;
            if (index < collection.Count) collection.Replay(collection.Pieces[index].pieceId);
        }

        private void Refresh()
        {
            page = Mathf.Clamp(page, 0, Mathf.Max(0, (collection.Count - 1) / replayButtons.Length));
            heading.text = "PIANO  /  " + collection.Count + " SCORE FRAGMENTS";
            for (int i = 0; i < replayButtons.Length; i++)
            {
                int index = page * replayButtons.Length + i;
                bool available = index < collection.Count;
                replayButtons[i].interactable = available;
                replayLabels[i].text = available ? collection.Pieces[index].displayName : "---";
            }
            previousButton.interactable = page > 0;
            nextButton.interactable = (page + 1) * replayButtons.Length < collection.Count;
        }
    }
}
