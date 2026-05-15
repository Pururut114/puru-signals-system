#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UdonSharp;

namespace PuruSignals.Editor
{
    // Fallback repair tool — normally program assets ship with the package.
    // Use if assets are missing: Tools > PSS > Repair Missing Program Assets
    public static class PSS_Setup
    {
        [MenuItem("Tools/PSS/Repair Missing Program Assets")]
        public static void RepairMissingProgramAssets()
        {
            Type paType = null;
            FieldInfo scriptField = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType("UdonSharp.UdonSharpProgramAsset");
                if (t == null) continue;
                paType = t;
                scriptField = t.GetField("sourceCsScript",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                break;
            }
            if (paType == null || scriptField == null)
            {
                Debug.LogWarning("[PSS] UdonSharpProgramAsset not found — is UdonSharp installed?");
                return;
            }

            // Collect all scripts that already have a program asset anywhere in the project
            var covered = new System.Collections.Generic.HashSet<MonoScript>();
            foreach (string paGuid in AssetDatabase.FindAssets("t:UdonSharpProgramAsset"))
            {
                var pa = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    AssetDatabase.GUIDToAssetPath(paGuid));
                if (pa == null) continue;
                var ms = scriptField.GetValue(pa) as MonoScript;
                if (ms != null) covered.Add(ms);
            }

            const string kOutput = "Assets/PuruSignals/ProgramAssets";
            int created = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:MonoScript"))
            {
                string csPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!csPath.Contains("com.pururut.pss") || csPath.Contains("/Editor/")) continue;

                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(csPath);
                Type type = script?.GetClass();
                if (type == null || !typeof(UdonSharpBehaviour).IsAssignableFrom(type) || type.IsAbstract)
                    continue;

                if (covered.Contains(script)) continue;

                if (!AssetDatabase.IsValidFolder("Assets/PuruSignals"))
                    AssetDatabase.CreateFolder("Assets", "PuruSignals");
                if (!AssetDatabase.IsValidFolder(kOutput))
                    AssetDatabase.CreateFolder("Assets/PuruSignals", "ProgramAssets");

                string assetPath = $"{kOutput}/{type.Name}.asset";
                var pa = (ScriptableObject)ScriptableObject.CreateInstance(paType);
                scriptField.SetValue(pa, script);
                AssetDatabase.CreateAsset(pa, assetPath);
                created++;
                Debug.Log($"[PSS] Repaired: {assetPath}");
            }

            if (created > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[PSS] Done — repaired {created} program assets.");
            }
            else
            {
                Debug.Log("[PSS] All program assets are present.");
            }
        }
    }
}
#endif
