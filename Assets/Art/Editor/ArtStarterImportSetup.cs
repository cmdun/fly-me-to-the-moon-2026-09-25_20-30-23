using UnityEditor;
using UnityEngine;

namespace FlyMeToTheMoon.Art.Editor
{
    /// <summary>Applies the project's native pixel-art import contract.</summary>
    public static class ArtStarterImportSetup
    {
        public static void Apply()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Sprites" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16f;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Applied pixel-art import settings to {guids.Length} art starter textures.");
        }
    }
}
