#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UdonSharp;

namespace PuruSignals.Editor
{
    [InitializeOnLoad]
    public static class PSS_Setup
    {
        const string kOutputFolder = "Assets/PuruSignals/ProgramAssets";

        static PSS_Setup()
        {
            EditorApplication.delayCall += CreateMissingProgramAssets;
        }

        [MenuItem("Tools/PSS/Create Missing Program Assets")]
        public static void CreateMissingProgramAssets()
        {
            Type programAssetType = null;
            FieldInfo scriptField  = null;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType("UdonSharp.UdonSharpProgramAsset");
                if (t == null) continue;
                programAssetType = t;
                scriptField = t.GetField("sourceCsScript",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                break;
            }

            if (programAssetType == null || scriptField == null)
            {
                Debug.LogWarning("[PSS Setup] UdonSharpProgramAsset not found — is UdonSharp installed?");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Packages/com.pururut.pss" });
            if (guids.Length == 0) return;

            // Quick exit: check if any program assets are missing
            bool needsCreate = false;
            foreach (string guid in guids)
            {
                string csPath = AssetDatabase.GUIDToAssetPath(guid);
                if (csPath.Contains("/Editor/")) continue;
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(csPath);
                Type type = script?.GetClass();
                if (type == null || !typeof(UdonSharpBehaviour).IsAssignableFrom(type) || type.IsAbstract) continue;
                if (!File.Exists($"{kOutputFolder}/{type.Name}.asset")) { needsCreate = true; break; }
            }
            if (!needsCreate) return;

            // Ensure output folder
            if (!AssetDatabase.IsValidFolder("Assets/PuruSignals"))
                AssetDatabase.CreateFolder("Assets", "PuruSignals");
            if (!AssetDatabase.IsValidFolder(kOutputFolder))
                AssetDatabase.CreateFolder("Assets/PuruSignals", "ProgramAssets");

            int created = 0;
            foreach (string guid in guids)
            {
                string csPath = AssetDatabase.GUIDToAssetPath(guid);
                if (csPath.Contains("/Editor/")) continue;

                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(csPath);
                Type type = script?.GetClass();
                if (type == null || !typeof(UdonSharpBehaviour).IsAssignableFrom(type) || type.IsAbstract) continue;

                string assetPath = $"{kOutputFolder}/{type.Name}.asset";
                if (File.Exists(assetPath)) continue;

                var programAsset = (ScriptableObject)ScriptableObject.CreateInstance(programAssetType);
                scriptField.SetValue(programAsset, script);
                AssetDatabase.CreateAsset(programAsset, assetPath);
                created++;
                Debug.Log($"[PSS Setup] Created: {assetPath}");
            }

            if (created > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[PSS Setup] Done — {created} program assets created in {kOutputFolder}");
            }
        }
    }

    public class PSS_AssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
                                            string[] movedAssets,    string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                if (path.StartsWith("Packages/com.pururut.pss/") &&
                    path.EndsWith(".cs") &&
                    !path.Contains("/Editor/"))
                {
                    EditorApplication.delayCall += PSS_Setup.CreateMissingProgramAssets;
                    return;
                }
            }
        }
    }
}
#endif
