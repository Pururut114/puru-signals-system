using UdonSharp;
using UnityEngine;

namespace PuruSignals
{
    public enum NodeSyncMode { Local, Global }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("PSS/PSS_Node [Node]")]
    public class PSS_Node : UdonSharpBehaviour
    {
        [HideInInspector] public NodeSyncMode syncMode = NodeSyncMode.Local;

        // Авто-заполняется PSS_NodeEditor при изменении в инспекторе.
        // Ссылка на Channel (Local или Global) на этом же объекте.
        [HideInInspector] public PSS_ChannelLocal _channel;

        private void Start()
        {
            if (_channel == null)
                _channel = GetComponent<PSS_ChannelLocal>();

            if (_channel == null) return;

            // Присваиваем channel всем Trigger'ам на этом объекте у которых channel не задан вручную.
            var triggers = GetComponents<PSS_TriggerBase>();
            for (int i = 0; i < triggers.Length; i++)
            {
                if (triggers[i] != null && triggers[i].channel == null)
                    triggers[i].channel = _channel;
            }
        }
    }
}
