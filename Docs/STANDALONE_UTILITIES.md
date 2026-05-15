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
