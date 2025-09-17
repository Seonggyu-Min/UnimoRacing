#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;



    public static class AssetNameCollector
    {
        public static List<string> GetAssetNames(string folderPath)
        {
            List<string> names = new List<string>();

            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { folderPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                names.Add(fileName);
            }

            return names;
        }
    }
#endif