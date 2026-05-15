# PSS — Network Architecture

## Концепция

Вместо hardcoded `RecieveNetworkFire0..9` (как в T23) — один `PSS_Network` в сцене как роутер.  
Все `PSS_ChannelGlobal` регистрируются в нём, получают числовой `networkId`. Нет лимита на количество глобальных каналов.

## Схема работы

```
PSS_ChannelGlobal.Trigger()
  → network.SendGlobalEvent(channelId, seed)
    → Networking.SetOwner(LocalPlayer, networkGO)
    → RequestSerialization()
      → OnDeserialization() у всех клиентов
        → channels[pendingId]._ReceiveNetworkFire(seed)
          → Random.InitState(seed)  ← одинаковый random у всех
          → _Fire()                 ← dispatch Actions
```

## PSS_Network API

| Метод | Кто вызывает | Что делает |
|-------|-------------|-----------|
| `RegisterChannel(channel)` | PSS_ChannelGlobal.Start() | Добавляет канал в реестр, возвращает networkId |
| `SendGlobalEvent(id, seed)` | PSS_ChannelGlobal | Пишет в synced vars, вызывает RequestSerialization |
| `OnDeserialization()` | Udon runtime (все клиенты) | Находит канал по id, вызывает _ReceiveNetworkFire |
| `OnPlayerJoined()` | VRChat | Owner повторяет RequestSerialization для late-joiners |

**Synced variables:**
- `_pendingId` — id канала последнего события
- `_pendingTick` — flip-flop счётчик (повторные события одного канала)
- `_randomSeed` — seed для Random.InitState у всех клиентов

## PSS_ChannelGlobal

Наследует `PSS_ChannelLocal`. Всё что работало локально — работает и тут.  
Переопределяет `Trigger()` / `TriggerWithPlayer()` — добавляет сетевой dispatch.

**Обязательно:** назначить `network` поле в инспекторе (ссылка на PSS_Network в сцене).

**bufferForLateJoin:** PSS_Network.OnPlayerJoined повторяет последнее событие для входящих. Достаточно для большинства случаев.

## Важные ограничения

- `PSS_Network` должен быть один на сцену
- У PSS_Network должен быть `UdonBehaviourSyncMode = Manual`
- PSS_Network владеет serialization — все ChannelGlobal используют один sync поток (sequentially ordered)
- Если два канала стреляют одновременно — второй перезаписывает первый в synced vars. Для VRChat миров это обычно не проблема (события редкие), но для высокочастотных систем лучше использовать LocalChannel

## Настройка в сцене

1. Создать пустой GameObject `PSS_Network`
2. Добавить компонент `PSS/Network/Network`
3. В каждом `PSS_ChannelGlobal` назначить это поле `network`
4. Убедиться что на PSS_Network есть VRCObjectSync или SyncMode = Manual

## Random Seed синхронизация

При каждом глобальном событии генерируется seed = `Random.Range(0, 100000)`.  
Передаётся через synced vars. На каждом клиенте перед `_Fire()` вызывается `Random.InitState(seed)`.  
Результат: weighted-random dispatch (`randomize = true`) даёт одинаковый результат у всех игроков.
