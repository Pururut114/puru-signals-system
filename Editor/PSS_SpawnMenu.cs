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

        [MenuItem("Tools/PSS/Spawn/Zones/Zone — Reparent Snap")]
        static void SpawnZoneReparentSnap()
        {
            var go = new GameObject("PSS_Zone_ReparentSnap");

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(4f, 3f, 4f);

            go.AddComponent<PSS_ZoneReparentSnap>();

            PlaceInSceneView(go);
            RegisterAndSelect(go, "Create PSS Zone Reparent Snap");
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

        // ── Persistence ──────────────────────────────────────────────────────

        [MenuItem("Tools/PSS/Spawn/Persistence/Position Persistence")]
        static void SpawnPositionPersistence()
        {
            var go = new GameObject("PSS_PositionPersistence");
            go.AddComponent<PSS_PositionPersistence>();
            PlaceInSceneView(go);
            RegisterAndSelect(go, "Create PSS Position Persistence");
        }

        // ── Teleport ─────────────────────────────────────────────────────────

        [MenuItem("Tools/PSS/Spawn/Teleport/Interact Teleport")]
        static void SpawnInteractTeleport()
        {
            var go = new GameObject("PSS_InteractTeleport");
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(1f, 2f, 0.1f);
            go.AddComponent<PSS_InteractTeleport>();
            PlaceInSceneView(go);
            RegisterAndSelect(go, "Create PSS Interact Teleport");
        }

        [MenuItem("Tools/PSS/Spawn/Teleport/Pickup Portal")]
        static void SpawnPickupPortal()
        {
            var go = new GameObject("PSS_PickupPortal");
            go.AddComponent<PSS_PickupPortal>();
            PlaceInSceneView(go);
            RegisterAndSelect(go, "Create PSS Pickup Portal");
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

        // ── Access ───────────────────────────────────────────────────────────

        [MenuItem("Tools/PSS/Spawn/Access/Admin Visibility")]
        static void SpawnAdminVisibility()
        {
            var go = new GameObject("PSS_AdminVisibility");
            go.AddComponent<PSS_AdminVisibility>();
            PlaceInSceneView(go);
            RegisterAndSelect(go, "Create PSS Admin Visibility");
        }

        [MenuItem("Tools/PSS/Spawn/Access/Admin Visibility Full")]
        static void SpawnAdminVisibilityFull()
        {
            var go = new GameObject("PSS_AdminVisibilityFull");
            go.AddComponent<PSS_AdminVisibilityFull>();
            PlaceInSceneView(go);
            RegisterAndSelect(go, "Create PSS Admin Visibility Full");
        }

        [MenuItem("Tools/PSS/Spawn/Access/Instance Owner Visibility")]
        static void SpawnInstanceOwnerVisibility()
        {
            var go = new GameObject("PSS_InstanceOwnerVisibility");
            go.AddComponent<PSS_InstanceOwnerVisibility>();
            PlaceInSceneView(go);
            RegisterAndSelect(go, "Create PSS Instance Owner Visibility");
        }

        [MenuItem("Tools/PSS/Spawn/Access/Master Visibility")]
        static void SpawnMasterVisibility()
        {
            var go = new GameObject("PSS_MasterVisibility");
            go.AddComponent<PSS_MasterVisibility>();
            PlaceInSceneView(go);
            RegisterAndSelect(go, "Create PSS Master Visibility");
        }

        [MenuItem("Tools/PSS/Spawn/Access/Zone — Admin Visibility")]
        static void SpawnZoneAdminVisibility()
        {
            var go = new GameObject("PSS_Zone_AdminVisibility");

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(4f, 3f, 4f);

            go.AddComponent<PSS_ZoneAdminVisibility>();

            PlaceInSceneView(go);
            RegisterAndSelect(go, "Create PSS Zone Admin Visibility");
        }

        [MenuItem("Tools/PSS/Spawn/Access/Zone — Admin Visibility Full")]
        static void SpawnZoneAdminVisibilityFull()
        {
            var go = new GameObject("PSS_Zone_AdminVisibilityFull");

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(4f, 3f, 4f);

            go.AddComponent<PSS_ZoneAdminVisibilityFull>();

            PlaceInSceneView(go);
            RegisterAndSelect(go, "Create PSS Zone Admin Visibility Full");
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

        [MenuItem("Tools/PSS/Spawn/ProTV/ProTV Ambient Fade")]
        static void SpawnProTVAmbientFade()
        {
            var go = new GameObject("PSS_ProTVAmbientFade");
            var type = FindType("PuruSignals.PSS_ProTVAmbientFade");
            if (type != null) go.AddComponent(type);
            PlaceInSceneView(go);
            RegisterAndSelect(go, "Create PSS ProTV Ambient Fade");
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
