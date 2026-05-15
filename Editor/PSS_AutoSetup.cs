#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UdonSharp;

namespace PuruSignals.Editor
{
    // Runs on every domain reload. Auto-manages PSS_PROTV_INSTALLED / PSS_LTCGI_INSTALLED
    // scripting defines based on whether the corresponding packages are loaded, then silently
    // creates any missing UdonSharpProgramAsset files when defines are already stable.
    [InitializeOnLoad]
    public static class PSS_AutoSetup
    {
        static PSS_AutoSetup()
        {
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (SyncDefines())
                return; // recompile triggered — repair runs on next domain reload

            RepairSilent();
        }

        // ── Define sync ───────────────────────────────────────────────────────

        static bool SyncDefines()
        {
            var target = EditorUserBuildSettings.selectedBuildTargetGroup;
#pragma warning disable CS0618
            var raw = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
#pragma warning restore CS0618
            var defines = new HashSet<string>(
                raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

            bool changed = false;
            changed |= SyncDefine(defines, "PSS_PROTV_INSTALLED", IsAssemblyLoaded("ArchiTech.ProTV.Runtime"));
            changed |= SyncDefine(defines, "PSS_LTCGI_INSTALLED",  IsAssemblyLoaded("LTCGI"));

            if (!changed) return false;

#pragma warning disable CS0618
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", defines));
#pragma warning restore CS0618
            return true;
        }

        static bool SyncDefine(HashSet<string> defines, string symbol, bool shouldExist)
        {
            if (shouldExist && defines.Add(symbol))
            {
                Debug.Log($"[PSS AutoSetup] Added define: {symbol}");
                return true;
            }
            if (!shouldExist && defines.Remove(symbol))
            {
                Debug.Log($"[PSS AutoSetup] Removed define: {symbol}");
                return true;
            }
            return false;
        }

        static bool IsAssemblyLoaded(string name)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                if (asm.GetName().Name == name) return true;
            return false;
        }

        // ── Program asset repair ──────────────────────────────────────────────

        static void RepairSilent()
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
            if (paType == null || scriptField == null) return;

            var covered = new HashSet<MonoScript>();
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
                Debug.Log($"[PSS AutoSetup] Created program asset: {assetPath}");
            }

            if (created > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[PSS AutoSetup] Repaired {created} missing program assets.");
            }
        }
    }
}
#endif
