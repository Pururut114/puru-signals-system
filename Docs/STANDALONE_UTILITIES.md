# PSS — Standalone Utilities

Самодостаточные компоненты. Не используют систему Trigger → Channel → Action.
Каждый решает одну конкретную задачу — ставишь на объект, настраиваешь, работает.

Добавляются на сцену через `Tools > PSS > Spawn > <Категория> > <Инструмент>`.

---

## Zones

### PSS_ZoneEnableWhileInside

**Спавн:** `Tools > PSS > Spawn > Zones > Zone — Enable While Inside`

Включает список объектов пока локальный игрок находится внутри trigger-зоны.
Каждый экземпляр независимо проверяет своё состояние при старте — корректно работает
когда игрок спаунится уже внутри зоны или далеко от неё.

**Поля:**

| Поле | Тип | По умолчанию | Описание |
|------|-----|-------------|----------|
| `targets` | `GameObject[]` | — | Объекты для включения/выключения |
| `invert` | `bool` | `false` | Включать когда СНАРУЖИ, выключать внутри |
| `zoneColliders` | `Collider[]` | — | Trigger-коллайдеры зоны. Пусто = автосбор с себя и детей (только `isTrigger = true`) |
| `evaluateOnStart` | `bool` | `true` | Проверить позицию игрока при старте |
| `startEvalDelayFrames` | `int` | `2` | Задержка старта в кадрах (LocalPlayer инициализируется не сразу) |

**Топология при спавне из меню:**
```
PSS_Zone_EnableWhileInside
├── BoxCollider  (isTrigger = true, size 4×3×4)
└── PSS_ZoneEnableWhileInside
```

**Типовые сетапы:**

```
// Один триггер-коллайдер на том же объекте — автосбор работает, zoneColliders оставить пустым

// Зона из нескольких коллайдеров — назначить все вручную в zoneColliders[]
// или сложить их как детей объекта со скриптом

// Invert = true — объекты активны снаружи зоны (например, ambient звук 
// который нужно выключить когда игрок заходит в тихую комнату)
```

**Когда использовать:**
- Окружение/эффекты видимые только из определённой зоны
- Дополнение к Unity Occlusion Culling — гарантированно убивать объекты вне зоны
- Ambient-эффекты, звуки, постпроцессинг привязанные к зоне
- Несколько зон на сцене — каждая работает независимо

---

### PSS_FallZoneBlackoutTeleport

**Спавн:** `Tools > PSS > Spawn > Zones > Fall Zone — Blackout Teleport`

Игрок входит в trigger-зону → fade to black (PPS v2) → телепорт → hold → fade обратно → PostFX выключается.
Пока идёт последовательность — повторный вход игнорируется.

**Поля:**

| Поле | Тип | По умолчанию | Описание |
|------|-----|-------------|----------|
| `postVolume` | `PostProcessVolume` | — | Volume с профилем затемнения. Пусто = взять с этого объекта |
| `postObject` | `GameObject` | — | Объект для enable/disable. Пусто = `postVolume.gameObject` |
| `teleportTarget` | `Transform` | — | Точка назначения телепорта |
| `teleportYOffset` | `float` | `0` | Смещение по Y после телепорта |
| `fadeInSeconds` | `float` | `1.0` | Время fade 0→1 |
| `holdSeconds` | `float` | `1.0` | Время держать полное затемнение |
| `extraDelayAfterBlack` | `float` | `0.5` | Задержка от начала полного затемнения до телепорта. Автоматически ограничивается `holdSeconds - 0.1` |
| `fadeOutSeconds` | `float` | `1.0` | Время fade 1→0 |
| `disableDelaySeconds` | `float` | `1.0` | Задержка перед выключением PostFX после fade-out |

**Топология при спавне из меню:**
```
PSS_Zone_BlackoutTeleport
├── BoxCollider  (isTrigger = true, size 4×2×4)
└── PSS_FallZoneBlackoutTeleport
    (postVolume и teleportTarget — назначить вручную)
```

**Тайминг последовательности:**
```
t=0                  → fade начинается (0→1 за fadeInSeconds)
t=fadeIn             → экран чёрный, startFadeOut запланирован через holdSeconds
t=fadeIn+safeExtra   → телепорт (safeExtra = min(extraDelayAfterBlack, holdSeconds-0.1))
t=fadeIn+hold        → fade обратно (1→0 за fadeOutSeconds)
t=fadeIn+hold+fadeOut+disableDelay → PostFX объект выключается, можно триггерить снова
```

**Когда использовать:**
- Fall zones — игрок падает за пределы карты и возвращается без резкого "прыжка"
- Порталы с переходом через затемнение
- Respawn точки с визуальным feedback

---

### PSS_ZoneReparentSnap

**Спавн:** `Tools > PSS > Spawn > Zones > Zone — Reparent Snap`

При входе локального игрока в зону reparent'ит `targetRoot` к `enterMarker` и опционально снапает локальные трансформы (position/rotation/scale → 0/identity/1). При выходе — к `exitMarker` (если задан).

**Поля:**

| Поле | Тип | По умолчанию | Описание |
|------|-----|-------------|----------|
| `targetRoot` | `Transform` | — | Объект для reparent/snap |
| `enterMarker` | `Transform` | — | Anchor при входе в зону |
| `exitMarker` | `Transform` | — | Anchor при выходе. Пусто = ничего не делать при выходе |
| `snapPosition` | `bool` | `true` | Обнулить localPosition после reparent |
| `snapRotation` | `bool` | `true` | Обнулить localRotation после reparent |
| `snapScale` | `bool` | `true` | Сбросить localScale в Vector3.one после reparent |
| `zoneColliders` | `Collider[]` | — | Trigger-коллайдеры зоны. Пусто = автосбор с себя и детей |
| `evaluateOnStart` | `bool` | `true` | Проверить позицию игрока при старте |
| `startEvalDelayFrames` | `int` | `2` | Задержка старта в кадрах |

**Логика Apply:**
```
targetRoot.SetParent(marker, false)
→ localPosition = Vector3.zero        (если snapPosition)
→ localRotation = Quaternion.identity (если snapRotation)
→ localScale    = Vector3.one         (если snapScale)
```

**Топология при спавне из меню:**
```
PSS_Zone_ReparentSnap
├── BoxCollider  (isTrigger = true, size 4×3×4)
└── PSS_ZoneReparentSnap (component)
```

**Когда использовать:**
- Зонально-зависимый UI — панель прикрепляется к нужному anchor при входе в комнату
- Объект "следует" за зонами — снапается к разным точкам при переходах
- Двусторонний snap: `enterMarker` при входе, `exitMarker` при выходе

---

## Persistence

### PSS_PositionPersistence

**Спавн:** `Tools > PSS > Spawn > Persistence > Position Persistence`

Сохраняет позицию и ротацию локального игрока между визитами в мир через VRChat PlayerData API. При следующем заходе восстанавливает игрока на последнее сохранённое место.

`_restored` флаг защищает от записи до того как `OnPlayerRestored` отработал — без этого первый тик SaveLoop перезапишет сейв дефолтной спаун-позицией.

**Поля:**

| Поле | Тип | По умолчанию | Описание |
|------|-----|-------------|----------|
| `keyPrefix` | `string` | `"PSS"` | Префикс ключей PlayerData. Итоговые ключи: `{prefix}-LastPos` / `{prefix}-LastRot`. PlayerData изолирован по мирам — менять только если несколько экземпляров в одном мире |
| `saveInterval` | `float` | `10` | Секунды между авто-сохранениями. Игнорируется при `useCheckpointsOnly = true` |
| `useCheckpointsOnly` | `bool` | `false` | Отключить авто-loop. Сохранения только по `OnCheckpointReached()` |
| `onRestoredChannel` | `PSS_ChannelLocal` | — | Канал, срабатывающий после восстановления позиции. Полезно для fade-in или других реакций |

**Методы:**
- `OnCheckpointReached()` — вызвать из внешних триггеров для сохранения позиции в конкретных точках
- `SaveCurrentPosition()` — явное сохранение (можно вызвать из PSS_CallMethod)

**PSS интеграция:**

`onRestoredChannel` срабатывает только когда есть сохранённые данные и игрок был перемещён. Первый визит (нет данных) — канал не файрится.

Пример: `onRestoredChannel → ChannelLocal → PSS_FadeOnJoin.Begin()` — запустить fade-in только для returning players.

**Когда использовать:**
- Большие открытые миры — игрок возвращается туда где был
- Лабиринты, dungeon-like миры с прогрессом по локации
- `useCheckpointsOnly = true` + чекпоинт-триггеры — сохранение только в безопасных точках

---

## Teleport

### PSS_InteractTeleport

**Спавн:** `Tools > PSS > Spawn > Teleport > Interact Teleport`

Teleport локального игрока по Interact с опциональным PostFX blackout переходом. Fade 0→1 → hold → teleport → 1→0.

**Поля:**

| Поле | Тип | По умолчанию | Описание |
|------|-----|-------------|----------|
| `destination` | `Transform` | — | Точка назначения телепорта |
| `fadeVolume` | `PostProcessVolume` | — | Volume для затемнения. Пусто = мгновенный телепорт |
| `fadeDuration` | `float` | `0.5` | Длительность fade in и fade out |
| `holdSeconds` | `float` | `0.2` | Задержка на полном затемнении перед телепортом |
| `triggerOnce` | `bool` | `false` | Отключить после первого использования |

**Топология при спавне из меню:**
```
PSS_InteractTeleport
├── BoxCollider  (size 1×2×0.1 — дверной проём)
└── PSS_InteractTeleport (component)
```

---

### PSS_PickupPortal

**Спавн:** `Tools > PSS > Spawn > Teleport > Pickup Portal`

Pickup-пульт: нажал Use → fade → teleport игрока → fade back → respawn пикапа на `remoteRespawnPoint`. Пикап сам телепортируется обратно к точке после использования.

**Поля:**

| Поле | Тип | По умолчанию | Описание |
|------|-----|-------------|----------|
| `playerDestination` | `Transform` | — | Точка назначения телепорта игрока |
| `remoteRespawnPoint` | `Transform` | — | Куда телепортируется сам пикап после использования. Пусто = остаётся на месте |
| `fadeVolume` | `PostProcessVolume` | — | Volume для затемнения. Пусто = мгновенный телепорт |
| `fadeDuration` | `float` | `0.5` | Длительность fade in и fade out |
| `holdSeconds` | `float` | `0.2` | Задержка на полном затемнении перед телепортом |
| `pickup` | `VRC_Pickup` | — | Авто-detect с этого объекта если пусто |
| `objectSync` | `VRCObjectSync` | — | Авто-detect с этого объекта если пусто |
| `requireHeld` | `bool` | `true` | Срабатывает только когда держит локальный игрок |
| `dropAfterUse` | `bool` | `true` | Drop() после использования |

**Порядок операций:**
```
OnPickupUseDown → fade 0→1 → teleport игрока → hold → fade 1→0
                                                             ↓
                                              drop pickup (если dropAfterUse)
                                              SetOwner → FlagDiscontinuity
                                              SetPositionAndRotation(remoteRespawnPoint)
                                              Rigidbody velocity = 0
```

**Топология при спавне из меню:**
```
PSS_PickupPortal
└── PSS_PickupPortal (component)
    (VRC_Pickup и VRCObjectSync — добавить на тот же объект,
     они подхватятся автоматически через GetComponent)
```

---

## FX

### PSS_FadeOnJoin

**Спавн:** `Tools > PSS > Spawn > FX > Fade On Join`

Fade PostProcessVolume weight с 1 до 0 при заходе в мир. Покрывает два кейса из двух разных реализаций: `forceDarkFrames` предотвращает flash в первые кадры, `holdSeconds` держит черный экран пока сцена грузится, затем плавно открывает картинку.

**Поля:**

| Поле | Тип | По умолчанию | Описание |
|------|-----|-------------|----------|
| `volume` | `PostProcessVolume` | — | Volume для фейда. Пусто = взять с этого объекта |
| `forceDarkFrames` | `int` | `3` | Форсит weight=1 первые N кадров. Предотвращает flash при инициализации сцены |
| `holdSeconds` | `float` | `1.0` | Секунды держать полное затемнение до старта фейда |
| `fadeDuration` | `float` | `2.0` | Длительность фейда 1→0 |
| `disableAfterFade` | `bool` | `true` | Выключить `volume.gameObject` после завершения. Освобождает GPU |
| `disableDelay` | `float` | `0.5` | Задержка перед выключением. Игнорируется если `disableAfterFade = false` |
| `autoStart` | `bool` | `true` | Запустить автоматически при `Start()` |

**Методы:**
- `Begin()` — запустить вручную (если `autoStart = false` или требуется повторный запуск после re-enable)

**Тайминг:**
```
t=0                  → Start() / Begin(): weight = 1, ждём forceDarkFrames кадров
t=forceDarkFrames    → _BeginHold: ждём holdSeconds секунд
t=hold done          → _BeginFade: старт фейда 1→0 за fadeDuration
t=fade done          → weight = 0, если disableAfterFade → disable через disableDelay
```

**Топология при спавне из меню:**
```
PSS_FadeOnJoin
└── PSS_FadeOnJoin (component)
    (назначить volume вручную или положить PostProcessVolume на тот же объект)
```

**Когда использовать:**
- Старт любого мира — скрыть загрузку/инициализацию объектов за черным экраном
- Более длинный `holdSeconds` для тяжёлых сцен с долгой загрузкой ассетов
- `autoStart = false` + `Begin()` из другого скрипта для отложенного старта

---

## Select

### PSS_MultiSelectController

**Спавн:** `Tools > PSS > Spawn > Select > Multi-Select Controller`

Синхронизированный (`BehaviourSyncMode.Manual`) radio-selector для массива GameObject.
Ровно один из `targets` включён по индексу, остальные выключены. `-1` = все выключены.

Первый владелец один раз устанавливает `defaultSelectedIndex` и сериализует флаг `initialized` — при смене владельца дефолты не сбрасываются. Остальные игроки получают состояние через `OnDeserialization`.

**Поля:**

| Поле | Тип | По умолчанию | Описание |
|------|-----|-------------|----------|
| `targets` | `GameObject[]` | — | Массив объектов. Включается ровно один |
| `defaultSelectedIndex` | `int` | `-1` | Начальный индекс при первом запуске. -1 = все выключены |
| `broadcastApplyForInstantFeedback` | `bool` | `true` | `SendCustomNetworkEvent(All, Apply)` — применяет состояние сразу, не дожидаясь десериализации |
| `onSelectChannels` | `PSS_ChannelLocal[]` | — | *PSS интеграция.* При выборе индекса N срабатывает канал N. Оставить пустым = только SetActive |

**Методы:**
- `SelectIndex(int)` — выбрать конкретный индекс
- `SelectToggle(int)` — выбрать если не выбран, снять если выбран (→ -1)
- `SelectNone()` — деактивировать всё (-1)
- `Apply()` — применить текущий `selectedIndex` (SetActive + канал). Вызывается из `OnDeserialization`

**PSS интеграция:**

`onSelectChannels[]` — массив каналов, индекс совпадает с `targets[]`. При выборе индекса N срабатывает `onSelectChannels[N].Trigger()`. Позволяет через стандартные PSS Action компоненты реагировать на смену выбора без дополнительных скриптов.

Каналы срабатывают внутри `Apply()` — в том числе на `OnDeserialization` у поздно подключившихся. Для одноразовых эффектов это нужно учитывать.

**Топология при спавне из меню:**
```
PSS_MultiSelectController
└── PSS_MultiSelectController (component)
```

**Типовые сетапы:**
```
// Рейв пульт: 3 пресета эффектов, взаимоисключающие
// targets[0] = root объектов эффекта A
// targets[1] = root объектов эффекта B
// targets[2] = root объектов эффекта C
// defaultSelectedIndex = -1 (старт — всё выключено)

// + PSS каналы для дополнительных реакций без доп. скриптов:
// onSelectChannels[0] → ChannelLocal → PSS_AnimationParam (запустить анимацию A)
// onSelectChannels[1] → ChannelLocal → PSS_AnimationParam (запустить анимацию B)
```

---

### PSS_MultiSelectButton

**Спавн:** `Tools > PSS > Spawn > Select > Multi-Select Button`

Кнопка для управления `PSS_MultiSelectController`. При Interact вызывает `SelectIndex`, `SelectToggle` или `SelectNone`.

**Поля:**

| Поле | Тип | По умолчанию | Описание |
|------|-----|-------------|----------|
| `controller` | `PSS_MultiSelectController` | — | Контроллер для управления |
| `indexToSelect` | `int` | `-1` | Индекс для выбора. -1 = SelectNone |
| `toggleMode` | `bool` | `false` | Если true: повторный клик на уже активный индекс деактивирует его (→ -1) |

**Методы:**
- `Interact()` — стандартный VRChat interact
- `Trigger()` — вызываемый из других Udon объектов

**Топология при спавне из меню:**
```
PSS_MultiSelectButton
├── BoxCollider  (size 0.5×0.5×0.1 — панельная кнопка)
└── PSS_MultiSelectButton (component)
```

**Когда использовать:**
- Рейв пульты: переключение между пресетами эффектов, сцен, состояний
- Любой tab panel паттерн — один элемент активен, остальные скрыты
- Физические кнопки на панелях с взаимоисключающим выбором

---

## Добавить новую утилиту

Краткий чеклист — полная инструкция в `Docs/ADDING_MODULES.md`:

- [ ] Файл в `Modules/Standalone Utilities/<Category>/PSS_<Name>.cs`
- [ ] Наследование от `UdonSharpBehaviour`, атрибут `[UdonBehaviourSyncMode]`
- [ ] `[AddComponentMenu("PSS/Standalone Utilities/<Category>/PSS_<Name> [Utility]")]`
- [ ] `[MenuItem]` в `Editor/PSS_SpawnMenu.cs` — создаёт объект с правильными defaults
- [ ] Строка в `Docs/modules.md`
- [ ] Секция в этом файле
