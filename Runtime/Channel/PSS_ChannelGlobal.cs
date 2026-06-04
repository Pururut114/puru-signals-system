using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    // Сетевой канал. Наследует PSS_ChannelLocal — все локальные Actions работают как обычно.
    // Дополнительно синхронизирует событие через PSS_Network → все клиенты получат _Fire().

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("PSS/Channel/PSS_ChannelGlobal [Channel]")]
    public class PSS_ChannelGlobal : PSS_ChannelLocal
    {
        [Header("Network")]
        [Tooltip("Ссылка на PSS_Network в сцене. Обязательна.")]
        public PSS_Network network;

        [Header("State Buffer (late join)")]
        [Tooltip("PSS_StateBuffer в сцене. Авто-заполняется при GlobalStateful режиме в PSS_Node.")]
        public PSS_StateBuffer stateBuffer;
        [Tooltip("0=None, 1=BufferOne (последнее событие), 2=Everytime (полная история для Toggle)")]
        public int bufferMode = 0;

        [Header("Restrictions")]
        [Tooltip("Только Instance Master может отправлять событие. Используй для OnTimer, OnSpawn — иначе каждый клиент шлёт независимо.")]
        public bool masterOnly = false;

        private int _networkId = -1;
        private int _bufferId  = -1;
        private int _lastSeed  = 0;

        // ── Инициализация ─────────────────────────────────────────────────────

        private void Start()
        {
            if (network != null)
                _networkId = network.RegisterChannel(this);
            else
                Debug.LogWarning($"[PSS] {name}: PSS_Network не назначен!");

            if (stateBuffer != null && bufferMode > 0)
                _bufferId = stateBuffer.RegisterChannel(this);
        }

        // ── Переопределяем Trigger — добавляем сетевой dispatch ───────────────

        public override void Trigger()
        {
            if (masterOnly && !Networking.IsMaster) return;
            triggeredPlayer = null;
            _SendToNetwork();
        }

        public override void TriggerWithPlayer(VRCPlayerApi player)
        {
            if (masterOnly && !Networking.IsMaster) return;
            triggeredPlayer = player;
            _SendToNetwork();
        }

        // Вызывается PSS_StateBuffer при replay для опоздавшего игрока
        public void _FireLocal()
        {
            _Fire();
        }

        private void _SendToNetwork()
        {
            if (stateBuffer != null && bufferMode > 0 && _bufferId >= 0)
                stateBuffer.RecordFire(_bufferId, bufferMode);

            if (network == null || _networkId < 0)
            {
                _Fire();
                return;
            }

            int seed = Random.Range(0, 100000);
            _lastSeed = seed;
            network.SendGlobalEvent(_networkId, seed);
            // Owner не получает OnDeserialization — выполняем локально сразу
            _ReceiveNetworkFire(seed);
        }

        // Вызывается PSS_Network при получении события от сети
        public void _ReceiveNetworkFire(int seed)
        {
            if (stateBuffer != null && bufferMode > 0)
                stateBuffer.NotifyApplied();
            Random.InitState(seed);
            _Fire();
            Random.InitState((int)(Time.time * 10000));
        }
    }
}
