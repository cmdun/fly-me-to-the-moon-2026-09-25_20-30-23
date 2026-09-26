using UnityEngine;

namespace FlyMeToTheMoon
{
    [ExecuteAlways, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class PrototypeShape : MonoBehaviour
    {
        public enum Shape { Circle, Rectangle }
        public Shape shape;
        public Vector2 size = Vector2.one;
        public Color color = Color.white;
        public Material material;
        [Range(4, 128)] public int segments = 96;
        private Mesh mesh;

        private void OnEnable() => Rebuild();
        public void Rebuild()
        {
            ReleaseMesh();
            mesh = new Mesh { name = "Procedural flat shape", hideFlags = HideFlags.DontSave };
            int count = shape == Shape.Circle ? segments : 4;
            var vertices = new Vector3[count + 1];
            var colors = new Color[count + 1];
            var triangles = new int[count * 3];
            colors[0] = color;
            for (int i = 0; i < count; i++)
            {
                if (shape == Shape.Circle)
                {
                    float angle = i * Mathf.PI * 2f / count;
                    vertices[i + 1] = new Vector3(Mathf.Cos(angle) * size.x, Mathf.Sin(angle) * size.y) * 0.5f;
                }
                else
                {
                    vertices[i + 1] = new Vector3(i == 0 || i == 3 ? -size.x : size.x,
                        i < 2 ? -size.y : size.y) * 0.5f;
                }
                colors[i + 1] = color;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % count + 1;
            }
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private void OnDisable() => ReleaseMesh();
        private void ReleaseMesh()
        {
            if (mesh == null) return;
            if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
            mesh = null;
        }
    }
}
