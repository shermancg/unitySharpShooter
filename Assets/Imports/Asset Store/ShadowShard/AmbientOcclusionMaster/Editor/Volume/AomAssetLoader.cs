using System.IO;
using UnityEditor;
using UnityEngine;

namespace ShadowShard.AmbientOcclusionMaster.Editor.Volume
{
    internal class AomAssetLoader
    {
        internal Texture2D LoadCoverImage()
        {
            string path = FindAmbientOcclusionMasterPath();
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("ShadowShard folder not found.");
                return null;
            }
            string filePath = FindFilePath(path, "ShadowShardAmbientOcclusionMasterLogo.png");
            return AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
        }

        internal void DrawAssetLogo(Texture2D coverImage)
        {
            if (coverImage == null) return;
            float availableWidth = EditorGUIUtility.currentViewWidth - 35f;
            float height = availableWidth / ((float)coverImage.width / coverImage.height);
            GUI.DrawTexture(GUILayoutUtility.GetRect(availableWidth, height), coverImage, ScaleMode.ScaleToFit);
        }
        
        private string FindAmbientOcclusionMasterPath()
        {
            string[] urpPlusPaths = AssetDatabase.FindAssets("Ambient Occlusion Master");
            foreach (string urpPlusGuid in urpPlusPaths)
            {
                string path = AssetDatabase.GUIDToAssetPath(urpPlusGuid);
                if (Directory.Exists(path))
                    return path;
            }

            return string.Empty;
        }

        private string FindFilePath(string folderPath, string filename)
        {
            string[] files = Directory.GetFiles(folderPath, filename, SearchOption.AllDirectories);
            return files.Length > 0 ? files[0] : string.Empty;
        }
    }
}