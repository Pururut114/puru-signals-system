#if UNITY_EDITOR
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UdonSharpEditor;

namespace PuruSignals.Editor
{
    // Запускается перед каждым билдом сцены.
    // Гарантирует что _actions[] на всех PSS_ChannelLocal заполнен корректно —
    // иначе RescanActions может не успеть записать в сцену до билда.
    public class PSS_BuildValidator : IProcessSceneWithReport
    {
        // callbackOrder < 0 чтобы запуститься до UdonSharp's own scene processing
        public int callbackOrder => -10;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var roots = scene.GetRootGameObjects();

            var channels = roots
                .SelectMany(go => go.GetComponentsInChildren<PSS_ChannelLocal>(true))
                .ToArray();

            if (channels.Length == 0) return;

            var allActions = roots
                .SelectMany(go => go.GetComponentsInChildren<PSS_ActionBase>(true))
                .ToArray();

            foreach (var channel in channels)
            {
                var linked = allActions
                    .Where(a => a != null && a.channel == channel)
                    .OrderBy(a => a.priority)
                    .ToArray();

                channel._actions = linked;
                UdonSharpEditorUtility.CopyProxyToUdon(channel);
            }

            Debug.Log($"[PSS Build] Populated _actions for {channels.Length} channel(s) in '{scene.name}'.");
        }
    }
}
#endif
