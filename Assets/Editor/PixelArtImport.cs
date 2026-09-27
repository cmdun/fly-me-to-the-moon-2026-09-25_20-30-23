using UnityEditor;
using UnityEngine;

namespace FlyMeToTheMoon.Editor
{
    // Batch entry point. Only this art pack's texture imports are touched; Unity owns all .meta files.
    public static class PixelArtImport
    {
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/PixelArt" }))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                importer.textureType = TextureImporterType.Default;
                importer.filterMode = FilterMode.Point; importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 4096;
                importer.isReadable = false; importer.SaveAndReimport(); count++;
            }
            Debug.Log("Pixel art import configured: " + count + " original sheets.");
        }
    }
}
