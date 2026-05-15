using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace PuruSignals
{
    // Единственный сетевой роутер PSS в сцене.
    // Все PSS_ChannelGlobal регистрируются здесь при Start() и получают числовой networkId.
    // Когда глобальный канал должен синхронизировать событие:
    //   1. Канал пишет свой id в _pendingId и данные события
    //   2. Вызывает RequestSerialization()
    //   3. Все клиенты получают OnDeserialization → RouteEvent(_pendingId)
    //
    // Один PSS_Network на сцену. Нужен Object Sync (Continuous) для работы.

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [AddComponentMenu("PSS/Network/PSS_Network [Network]")]
    public class PSS_Network : PSS_ModuleBase
    {
        // ── Синкованные переменные ────────────────────────────────────────────

        [UdonSynced] private int  _pendingId   = -1;
        [UdonSynced] private int  _pendingTick = 0;  // флип-флоп для повторных событий одного канала
        [UdonSynced] private int  _randomSeed  = 0;  // seed для воспроизводимого random на всех клиентах

        // ── Реестр каналов ────────────────────────────────────────────────────

        private PSS_ChannelGlobal[] _channels    = new PSS_ChannelGlobal[0];
        private int                 _channelCount = 0;

        // ─────────────────────────────────────────────────────────────────────

        public int RegisterChannel(PSS_ChannelGlobal channel)
        {
            // Расширяем массив
            var newArr = new PSS_ChannelGlobal[_channelCount + 1];
            for (int i = 0; i < _channelCount; i++) newArr[i] = _channels[i];
            newArr[_channelCount] = channel;
            _channels = newArr;
            return _channelCount++;
        }

        // Вызывается от PSS_ChannelGlobal когда нужно разослать событие всем.
        // Захватываем ownership и сериализуем — OnDeserialization сработает у всех остальных клиентов.
        // Локальный Fire вызывает сам ChannelGlobal сразу после этого метода.
        public void SendGlobalEvent(int channelId, int seed)
        {
            _pendingId   = channelId;
            _randomSeed  = seed;
            _pendingTick = (_pendingTick + 1) % 1000;

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        // ── Получение на клиентах ─────────────────────────────────────────────

        public override void OnDeserialization()
        {
            if (_pendingId < 0 || _pendingId >= _channelCount) return;
            _channels[_pendingId]._ReceiveNetworkFire(_randomSeed);
        }

        // ── Buffering для late-joiners ────────────────────────────────────────
        // Простая буферизация: при входе игрока Owner воспроизводит последнее состояние
        // через повторную RequestSerialization (OnDeserialization получат все новые клиенты).

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!Networking.IsOwner(gameObject)) return;
            if (_pendingId >= 0)
                RequestSerialization();
        }
    }
}
