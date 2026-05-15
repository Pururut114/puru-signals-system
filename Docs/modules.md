# PSS — Реестр модулей

Полный список реализованных модулей по пакетам.
Обновлять при добавлении каждого нового модуля.

Формат: `PSS_ИмяКласса` — одна строка описания

---

## Core (ядро)

### Triggers
| Класс | Описание |
|-------|----------|
| `PSS_OnInteract` | Локальный игрок кликает на объект |
| `PSS_OnEnterTrigger` | Игрок входит в Trigger Collider |
| `PSS_OnExitTrigger` | Игрок выходит из Trigger Collider |
| `PSS_OnTimer` | Срабатывает по таймеру (repeat, random interval) |
| `PSS_OnSpawn` | Один раз при старте сцены / старте объекта |
| `PSS_OnEnable` | При включении GameObject |
| `PSS_OnDisable` | При выключении GameObject |
| `PSS_CustomTrigger` | Именной триггер, вызывается по ссылке или имени |
| `PSS_ConditionalTrigger` | Срабатывает когда DataSlot удовлетворяет условию |

### Actions
| Класс | Описание |
|-------|----------|
| `PSS_SetActive` | Включить / выключить / Toggle GameObject |
| `PSS_AnimationParam` | Установить параметр аниматора (Trigger/Bool/Int/Float) |
| `PSS_CallMethod` | Вызвать публичный метод на UdonBehaviour |
| `PSS_ActiveCustomTrigger` | Активировать PSS_CustomTrigger по ссылке или имени |
| `PSS_SetDataSlot` | Записать значение в DataSlot (Set/Add/Sub/Mul/Div) |
| `PSS_SetStateSync` | Записать значение в PSS_StateSync (SetTrue/False/Toggle, Set/Add/Sub) |

### Data / Network
| Класс | Описание |
|-------|----------|
| `PSS_DataSlot` | Локальный контейнер данных (Bool/Int/Float/Vector3/String). Уведомляет ConditionalTrigger при изменении |
| `PSS_StateSync` | Синхронизированное состояние (Bool/Int/Float). Late-join корректен: хранит реальное значение, не историю событий |
| `PSS_Network` | Сетевой диспетчер. Один на сцену. Требуется для PSS_ChannelGlobal |

---

## Standalone Utilities

Самодостаточные скрипты — не Trigger/Action, не требуют канала. Добавляются через `Tools > PSS > Spawn`.

### Zones
| Класс | Описание |
|-------|----------|
| `PSS_ZoneEnableWhileInside` | Включает/выключает объекты пока локальный игрок внутри trigger-зоны. Поддерживает invert-режим и стартовую проверку позиции |
| `PSS_FallZoneBlackoutTeleport` | При входе в зону: fade to black (PPS v2) → телепорт → fade back. Защита от re-trigger во время анимации |

### Teleport
| Класс | Описание |
|-------|----------|
| `PSS_InteractTeleport` | Teleport с fade по Interact. `triggerOnce`, опциональный PostProcessVolume |
| `PSS_PickupPortal` | Pickup-пульт: нажал Use → fade → teleport → respawn пикапа в remoteRespawnPoint. `requireHeld`, `dropAfterUse` |

### FX
| Класс | Описание |
|-------|----------|
| `PSS_FadeOnJoin` | Fade PostProcessVolume weight 1→0 при старте мира. Hold → fade. `forceDarkFrames` защита от flash, `autoStart` или ручной `Begin()` |

### Select
| Класс | Описание |
|-------|----------|
| `PSS_MultiSelectController` | Synced radio-selector (Manual): ровно один из targets включён по индексу. -1 = все выключены. `SelectToggle`, `broadcastApplyForInstantFeedback`, опциональные `onSelectChannels` |
| `PSS_MultiSelectButton` | Кнопка для PSS_MultiSelectController. Выбирает индекс или SelectNone. Поддерживает toggleMode |

---

## Physics

### Actions
| Класс | Описание |
|-------|----------|
| `PSS_MoveToPoint` | Телепортирует Transform объекта к целевой точке (позиция + опциональная ротация) |

## Player

### Actions
| Класс | Описание |
|-------|----------|
| `PSS_TeleportPlayer` | Телепортирует локального игрока к Transform target с опциональным Y offset |

## Audio
_не реализован_

## UI
_не реализован_

## Pickup

### Actions
| Класс | Описание |
|-------|----------|
| `PSS_SetPickupable` | Включить / выключить возможность поднять VRC_Pickup объект. Drop() при выключении |

## Pool
_не реализован_

## Avatar

### Actions
| Класс | Описание |
|-------|----------|
| `PSS_SetAvatarScale` | Меняет рост локального игрока. Два режима: точный рост (world-authoritative) или min/max диапазон (player-controlled) |

## MIDI
_не реализован_

## Video
_не реализован_

## LTCGI

### Actions
| Класс | Описание |
|-------|----------|
| `PSS_LtcgiControl` | Включить / выключить / Toggle LTCGI — глобально или по списку экранов |

---

## ProTV *(условная сборка — требует `PSS_PROTV_INSTALLED`)*

### Standalone Utilities
| Класс | Описание |
|-------|----------|
| `PSS_ProTVAccessGate` | Access gate на основе ProTV whitelist/TVManager. Перемещает панель, ограничивает скейл аватара, управляет объектами/коллайдерами/пикапами по уровню доступа |
