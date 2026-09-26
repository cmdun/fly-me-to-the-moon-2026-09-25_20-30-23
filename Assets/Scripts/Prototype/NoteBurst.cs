using System.Collections;
using UnityEngine;

namespace FlyMeToTheMoon
{
    public sealed class NoteBurst : MonoBehaviour
    {
        public Material material;
        public int BurstCount { get; private set; }
        public int ActiveNotes { get; private set; }

        public void Emit(Vector2 up)
        {
            BurstCount++;
            StartCoroutine(AnimateNotes(up));
        }

        private IEnumerator AnimateNotes(Vector2 up)
        {
            var notes = new GameObject[6];
            var velocities = new Vector3[6];
            for (int i = 0; i < notes.Length; i++)
            {
                notes[i] = new GameObject("Flute note");
                notes[i].transform.position = transform.position + new Vector3(0f, 0f, -0.5f);
                Color color = i % 2 == 0 ? new Color(0.4f, 1f, 0.85f) : new Color(1f, 0.82f, 0.35f);
                AddPart(notes[i].transform, Vector2.zero, new Vector2(0.22f, 0.14f), color);
                AddPart(notes[i].transform, new Vector2(0.08f, 0.18f), new Vector2(0.06f, 0.36f), color);
                AddPart(notes[i].transform, new Vector2(0.17f, 0.33f), new Vector2(0.2f, 0.06f), color);
                float angle = i * Mathf.PI * 2f / notes.Length;
                velocities[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.8f + (Vector3)up;
            }
            ActiveNotes += notes.Length;
            float elapsed = 0f;
            while (elapsed < 0.65f)
            {
                elapsed += Time.deltaTime;
                for (int i = 0; i < notes.Length; i++)
                {
                    notes[i].transform.position += velocities[i] * Time.deltaTime;
                    notes[i].transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.1f, elapsed / 0.65f);
                }
                yield return null;
            }
            foreach (var note in notes) Destroy(note);
            ActiveNotes -= notes.Length;
        }

        private void AddPart(Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var part = new GameObject("Note stroke");
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            var shape = part.AddComponent<PrototypeShape>();
            shape.shape = PrototypeShape.Shape.Rectangle;
            shape.size = size;
            shape.color = color;
            shape.material = material;
            shape.Rebuild();
        }
    }
}
