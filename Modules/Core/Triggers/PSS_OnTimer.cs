using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Trigger/OnTimer")]
    [AddComponentMenu("PSS/Triggers/PSS_OnTimer [Trigger]")]
    public class PSS_OnTimer : PSS_TriggerBase
    {
        [PSS_Field("Interval (sec)", min: 0.01f)]
        public float interval = 1f;

        [PSS_Field("Repeat")]
        public bool repeat = true;

        [PSS_Field("Random Range", tooltip: "Случайный интервал между Interval и Max Interval")]
        public bool randomRange = false;

        [PSS_Field("Max Interval (sec)", min: 0.01f, showIf: "randomRange")]
        public float intervalMax = 2f;

        [PSS_Field("Start Delay (sec)", min: 0f)]
        public float startDelay = 0f;

        private bool _running = true;

        private void Start()
        {
            float delay = startDelay > 0f ? startDelay : NextInterval();
            SendCustomEventDelayedSeconds(nameof(_Tick), delay);
        }

        public void _Tick()
        {
            if (!_running) return;
            Fire();
            if (repeat)
                SendCustomEventDelayedSeconds(nameof(_Tick), NextInterval());
        }

        private float NextInterval()
        {
            return randomRange
                ? Random.Range(interval, Mathf.Max(interval, intervalMax))
                : interval;
        }

        // Вызвать из другого Udon-скрипта для остановки таймера
        public void Stop()  { _running = false; }
        public void Resume() { if (!_running) { _running = true; SendCustomEventDelayedSeconds(nameof(_Tick), NextInterval()); } }
    }
}
