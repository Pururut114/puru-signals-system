using UdonSharp;
using UnityEngine;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("PSS/Standalone Utilities/FX/PSS_MaterialCycler [Utility]")]
    public class PSS_MaterialCycler : UdonSharpBehaviour
    {
        [Header("Setup")]
        public MeshRenderer targetRenderer;
        public Material[] materials;
        [Header("Settings")]
        public float interval = 5f;
        public bool autoStart = true;

        private int _lastIndex = -1;
        private bool _running;

        private void Start()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<MeshRenderer>();
            if (autoStart) Begin();
        }

        public void Begin()
        {
            _running = true;
            Next();
        }

        public void Stop()
        {
            _running = false;
        }

        public void Next()
        {
            if (!_running || materials.Length == 0) return;

            int index;
            do { index = Random.Range(0, materials.Length); }
            while (materials.Length > 1 && index == _lastIndex);

            _lastIndex = index;
            targetRenderer.sharedMaterial = materials[index];
            SendCustomEventDelayedSeconds(nameof(Next), interval);
        }
    }
}
