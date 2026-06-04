using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    // Аналог T23_CommonBuffer.
    // Хранит историю срабатываний Global каналов (в порядке). При джоине нового
    // игрока воспроизводит историю — Actions re-execute, объекты оказываются в
    // правильном состоянии. Не требует synced полей в Actions.

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [AddComponentMenu("PSS/Network/PSS_StateBuffer [Network]")]
    public class PSS_StateBuffer : UdonSharpBehaviour
    {
        private const int MaxChannels = 64;
        private const int MaxHistory  = 128;

        private PSS_ChannelGlobal[] _channels = new PSS_ChannelGlobal[MaxChannels];
        private int _channelCount = 0;

        [UdonSynced] private int[] _history      = new int[MaxHistory];
        [UdonSynced] private int   _historyCount = 0;

        // Сколько событий этот клиент уже применил через real-time путь (PSS_Network).
        // OnDeserialization воспроизводит только события начиная с этого индекса.
        private int _appliedUpTo = 0;

        // ── Registration ──────────────────────────────────────────────────────

        public int RegisterChannel(PSS_ChannelGlobal channel)
        {
            if (_channelCount >= MaxChannels) return -1;
            int id = _channelCount;
            _channels[id] = channel;
            _channelCount++;
            return id;
        }

        // ── Record (вызывается PSS_ChannelGlobal при каждом срабатывании) ─────

        public void RecordFire(int channelId, int bufferMode)
        {
            if (bufferMode == 1) // BufferOne — оставить только последнее событие канала
            {
                for (int i = 0; i < _historyCount; i++)
                {
                    if (_history[i] != channelId) continue;
                    for (int j = i; j < _historyCount - 1; j++)
                        _history[j] = _history[j + 1];
                    _historyCount--;
                    if (_appliedUpTo > i) _appliedUpTo--;
                    break;
                }
            }

            if (_historyCount < MaxHistory)
                _history[_historyCount++] = channelId;

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            // RequestSerialization НЕ вызывается здесь — только при OnPlayerJoined.
            // Это предотвращает полный replay на уже синхронизированных клиентах.
        }

        // Вызывается PSS_ChannelGlobal._ReceiveNetworkFire() на каждом клиенте
        // при получении real-time события через PSS_Network.
        public void NotifyApplied()
        {
            _appliedUpTo++;
        }

        // ── Late Join ─────────────────────────────────────────────────────────

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!Networking.IsOwner(gameObject)) return;
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            // Воспроизводим только события которые этот клиент ещё не видел.
            for (int i = _appliedUpTo; i < _historyCount; i++)
            {
                int id = _history[i];
                if (id >= 0 && id < _channelCount && _channels[id] != null)
                    _channels[id]._FireLocal();
            }
            _appliedUpTo = _historyCount;
        }
    }
}
