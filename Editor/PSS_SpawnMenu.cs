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

        // ── Helpers ───────────────────────────────────────────────────────────

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
