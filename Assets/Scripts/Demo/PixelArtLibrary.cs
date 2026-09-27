using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    /// <summary>Reviewed, alpha-trimmed regions of the original art sheets. No collider geometry is generated.</summary>
    public static class PixelArtLibrary
    {
        [Serializable] public sealed class Region
        {
            public string key, sheet;
            public int x, y, width, height;
            public float pivotX, pivotY, ppu;
        }
        [Serializable] sealed class Catalog { public Region[] sprites; }
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static Dictionary<string, Region> regions;
        static Material material;
        public static IReadOnlyDictionary<string, Region> Regions { get { Load(); return regions; } }
        static void Load()
        {
            if (regions != null) return;
            var json = Resources.Load<TextAsset>("PixelArt/catalog");
            if (json == null) throw new InvalidOperationException("Missing PixelArt/catalog.json");
            regions = new Dictionary<string, Region>();
            foreach (var region in JsonUtility.FromJson<Catalog>(json.text).sprites) regions.Add(region.key, region);
        }
        public static Sprite Get(string key)
        {
            Load();
            if (sprites.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            var region = regions[key];
            var texture = Resources.Load<Texture2D>("PixelArt/" + region.sheet);
            if (texture == null) throw new InvalidOperationException("Missing pixel art: " + region.sheet);
            texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
            sprite = Sprite.Create(texture, new Rect(region.x, region.y, region.width, region.height),
                new Vector2(region.pivotX, region.pivotY), region.ppu, 0, SpriteMeshType.FullRect,
                Vector4.zero, false);
            sprite.name = key; sprites[key] = sprite; return sprite;
        }
        public static Sprite Moon(int index) => Get("world/" + (index == 0 ? 0 : 1 + (index - 1) % 6));
        public static SpriteRenderer Create(Transform parent, string name, string key, int order = 0)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = Get(key); renderer.sortingOrder = order;
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                material = new Material(shader) { name = "Pixel art — unlit" };
            }
            renderer.sharedMaterial = material; return renderer;
        }
        public static void Height(SpriteRenderer renderer, float height)
            => renderer.transform.localScale = Vector3.one * (height / renderer.sprite.bounds.size.y);
        public static void Size(SpriteRenderer renderer, Vector2 size)
            => renderer.transform.localScale = new Vector3(size.x / renderer.sprite.bounds.size.x, size.y / renderer.sprite.bounds.size.y, 1);
        public static void HideMeshes(Transform parent)
        {
            foreach (var renderer in parent.GetComponentsInChildren<MeshRenderer>(true)) renderer.enabled = false;
        }
    }
}
