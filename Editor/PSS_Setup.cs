#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UdonSharp;

namespace PuruSignals.Editor
{
    // Создаёт UdonSharpProgramAsset (.asset) рядом с каждым PSS runtime-скриптом.
    // Запустить вручную: Tools → PSS → Setup
    // Запускается автоматически при импорте скриптов Puru_Signals_System.

    public static class PSS_Setup
    {
        static readonly FieldInfo s_scriptField = typeof(UdonSharpProgramAsset).GetField(
            "sourceCsScript",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        [MenuItem("Tools/PSS/Setup — Create Missing Program Assets")]
        public static void CreateMissingProgramAssets()
        {
            if (s_scriptField == null)
            {
                Debug.LogError("[PSS Setup] Не удалось найти поле sourceCsScript в UdonSharpProgramAsset. " +
                               "Убедись что UdonSharp установлен.");
                return;
            }

            int created = 0;
            int fixed_  = 0;
            string[] guids = AssetDatabase.FindAssets("t:MonoScript");

            foreach (string guid in guids)
            {
                string csPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!csPath.Contains("Puru_Signals_System")) continue;
                if (csPath.Contains("/Editor/"))             continue;

                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(csPath);
                if (script == null) continue;

                Type type = script.GetClass();
                if (type == null)                                        continue;
                if (!typeof(UdonSharpBehaviour).IsAssignableFrom(type)) continue;
                if (type.IsAbstract)                                     continue;

                string assetPath = Path.ChangeExtension(csPath, ".asset");
                UdonSharpProgramAsset existing = AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(assetPath);

                if (existing != null)
                {
                    // Починить если sourceCsScript == null
                    if (s_scriptField.GetValue(existing) == null)
                    {
                        s_scriptField.SetValue(existing, script);
                        EditorUtility.SetDirty(existing);
                        fixed_++;
                        Debug.Log($"[PSS Setup] Fixed: {assetPath}");
                    }
                    continue;
                }

                // Создать новый — ставим поле ДО CreateAsset, чтобы оно попало в начальную сериализацию
                UdonSharpProgramAsset programAsset = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
                s_scriptField.SetValue(programAsset, script);
                AssetDatabase.CreateAsset(programAsset, assetPath);
                EditorUtility.SetDirty(programAsset);
                created++;
                Debug.Log($"[PSS Setup] Created: {assetPath}");
            }

            if (created > 0 || fixed_ > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[PSS Setup] Готово — создано: {created}, починено: {fixed_}. UdonSharp перекомпилирует.");
            }
            else
            {
                Debug.Log("[PSS Setup] Всё в порядке — все program assets существуют и привязаны.");
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
                if (path.Contains("Puru_Signals_System") &&
                    path.EndsWith(".cs")                 &&
                    !path.Contains("/Editor/"))
                {
                    PSS_Setup.CreateMissingProgramAssets();
                    return;
                }
            }
        }
    }
}
#endif
