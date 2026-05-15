#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace PuruSignals.Editor
{
    public static class PSS_SpawnMenu
    {
        // ── Zones ─────────────────────────────────────────────────────────────

        [MenuItem("Tools/PSS/Spawn/Zones/Zone — Enable While Inside")]
        static void SpawnZoneEnableWhileInside()
        {
            var go = new GameObject("PSS_Zone_EnableWhileInside");

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(4f, 3f, 4f);

            go.AddComponent<PSS_ZoneEnableWhileInside>();

            PlaceInSceneView(go);
            RegisterAndSelect(go, "Create PSS Zone Enable While Inside");
        }

        [MenuItem("Tools/PSS/Spawn/Zones/Fall Zone — Blackout Teleport")]
        static void SpawnFallZoneBlackoutTeleport()
        {
            var go = new GameObject("PSS_Zone_BlackoutTeleport");

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(4f, 2f, 4f);

            go.AddComponent<PSS_FallZoneBlackoutTeleport>();

            PlaceInSceneView(go);
            RegisterAndSelect(go, "Create PSS Fall Zone Blackout Teleport");
        }

        // ── FX ───────────────────────────────────────────────────────────────

        [MenuItem("Tools/PSS/Spawn/FX/Fade On Join")]
        static void SpawnFadeOnJoin()
        {
            var go = new GameObject("PSS_FadeOnJoin");
            go.AddComponent<PSS_FadeOnJoin>();
            PlaceInSceneView(go);
            RegisterAndSelect(go, "Create PSS Fade On Join");
        }

        // ── Select ───────────────────────────────────────────────────────────

        [MenuItem("Tools/PSS/Spawn/Select/Multi-Select Controller")]
        static void SpawnMultiSelectController()
        {
            var go = new GameObject("PSS_MultiSelectController");
            go.AddComponent<PSS_MultiSelectController>();
            PlaceInSceneView(go);
            RegisterAndSelect(go, "Create PSS Multi-Select Controller");
        }

        [MenuItem("Tools/PSS/Spawn/Select/Multi-Select Button")]
        static void SpawnMultiSelectButton()
        {
            var go = new GameObject("PSS_MultiSelectButton");
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(0.5f, 0.5f, 0.1f);
            go.AddComponent<PSS_MultiSelectButton>();
            PlaceInSceneView(go);
            RegisterAndSelect(go, "Create PSS Multi-Select Button");
        }

        // ── ProTV (conditional) ───────────────────────────────────────────────

#if PSS_PROTV_INSTALLED
        [MenuItem("Tools/PSS/Spawn/ProTV/ProTV Access Gate")]
        static void SpawnProTVAccessGate()
        {
            var go = new GameObject("PSS_ProTVAccessGate");
            var type = FindType("PuruSignals.PSS_ProTVAccessGate");
            if (type != null) go.AddComponent(type);
            PlaceInSceneView(go);
            RegisterAndSelect(go, "Create PSS ProTV Access Gate");
        }
#endif

        // ── Helpers ───────────────────────────────────────────────────────────

        static System.Type FindType(string fullName)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }

        static void PlaceInSceneView(GameObject go)
        {
            SceneView sv = SceneView.lastActiveSceneView;
            if (sv != null)
                go.transform.position = sv.pivot;
        }

        static void RegisterAndSelect(GameObject go, string undoName)
        {
            Undo.RegisterCreatedObjectUndo(go, undoName);
            Selection.activeGameObject = go;
        }
    }
}
#endif
