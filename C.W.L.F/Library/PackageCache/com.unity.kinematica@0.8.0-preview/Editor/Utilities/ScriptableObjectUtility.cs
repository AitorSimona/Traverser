using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Kinematica
{
    internal static class ScriptableObjectUtility
    {
        /// <summary>
        //  This makes it easy to create, name and place unique new ScriptableObject asset files.
        /// </summary>
        public static T CreateAsset<T>(string name = null, string extension = null) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "")
            {
                path = "Assets";
            }
            else if (Path.GetExtension(path) != "")
            {
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }

            string assetPathAndName;
            string ext = string.IsNullOrEmpty(extension) ? "asset" : extension;
            if (string.IsNullOrEmpty(name))
            {
                assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New " + typeof(T) + "." + ext);
            }
            else
            {
                assetPathAndName = path + "/" + name + "." + ext;
            }

            AssetDatabase.CreateAsset(asset, assetPathAndName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();

            return asset;
        }

        public static string GetSelectedPathOrFallback()
        {
            string path = "Assets";

            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!String.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                    break;
                }
            }
            return path;
        }
    }
}
