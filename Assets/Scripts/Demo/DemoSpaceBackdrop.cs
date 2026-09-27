using System.Collections.Generic;
using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    /// <summary>A camera-sized, repeating sky with four depths, owned entirely by the current world.</summary>
    [DefaultExecutionOrder(1000)]
    public sealed class DemoSpaceBackdrop : MonoBehaviour
    {
        Camera view;
        Material material;
        Mesh mesh;
        readonly List<Texture2D> textures = new List<Texture2D>();
        System.Random random;
        float age, nextMeteor, meteorStarted = -10;
        Vector2 meteorStart, meteorCamera;

        public void Build(Camera camera, int seed)
        {
            view = camera;
            random = new System.Random(seed ^ 0x73a61);
            material = new Material(Resources.Load<Shader>("PixelArt/SpaceBackdrop")) { name = "Layered pixel space" };
            AddTexture("_Nebula", DemoSpacePixels.Nebula(seed));
            AddTexture("_DeepStars", DemoSpacePixels.Stars(seed ^ 131, 1024, 900, 1, true));
            AddTexture("_MiddleStars", DemoSpacePixels.Stars(seed ^ 719, 1024, 260, 2, false));
            AddTexture("_NearStars", DemoSpacePixels.Stars(seed ^ 997, 512, 36, 3, false));
            mesh = new Mesh { name = "Space backdrop quad" };
            mesh.vertices = new[] { new Vector3(-1,-1), new Vector3(1,-1), new Vector3(1,1), new Vector3(-1,1) };
            mesh.triangles = new[] { 0,2,1,0,3,2 };
            mesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = -100;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            nextMeteor = 5 + (float)random.NextDouble() * 4;
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += BeforeCamera;
            PositionSky();
        }

        void AddTexture(string key, Texture2D texture)
        { textures.Add(texture); material.SetTexture(key, texture); }

        void LateUpdate()
        {
            if (view == null || material == null) return;
            if (Time.timeScale <= 0) { PositionSky(); return; }
            age += Time.deltaTime;
            if (age >= nextMeteor)
            {
                meteorCamera = view.transform.position;
                float height = view.orthographicSize;
                meteorStart = meteorCamera + new Vector2(-height * view.aspect * .5f, height * .7f);
                meteorStarted = age;
                nextMeteor = age + 12 + (float)random.NextDouble() * 9;
            }
            material.SetFloat("_SkyTime", age);
            float elapsed = age - meteorStarted;
            Vector2 head = meteorStart + ((Vector2)view.transform.position - meteorCamera) * .92f
                + new Vector2(2.1f,-.7f) * elapsed;
            float visibility = Mathf.SmoothStep(0,1,elapsed * 3)
                * (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.4f,2.3f,elapsed)));
            material.SetVector("_Meteor", new Vector4(head.x, head.y, visibility, 0));
            PositionSky();
        }

        void BeforeCamera(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
        { if (camera == view) PositionSky(); }

        void PositionSky()
        {
            if (view == null || material == null) return;
            // Match the final pixel-perfect camera size too, including sudden zoom and aspect changes.
            transform.position = view.transform.position + view.transform.forward * 20;
            transform.rotation = view.transform.rotation;
            transform.localScale = new Vector3(view.orthographicSize * view.aspect + 1, view.orthographicSize + 1, 1);
            material.SetVector("_ViewPosition", view.transform.position);
        }

        void OnDestroy()
        {
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= BeforeCamera;
            if (material != null) Destroy(material);
            if (mesh != null) Destroy(mesh);
            foreach (var texture in textures) if (texture != null) Destroy(texture);
        }
    }
}
