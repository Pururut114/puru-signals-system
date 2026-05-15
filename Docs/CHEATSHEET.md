# PSS Cheatsheet — Быстрый справочник

Краткое описание каждого модуля + один практический пример с сетапом.
Обновлять при добавлении новых модулей.

---

## Quick Start

### 1. Установка
Скопируй папку `Puru_Signals_System/` в `Assets/` своего Unity проекта.
Требования: **VRChat SDK3** + **UdonSharp** уже установлены в проекте.

После копирования обязательно запусти **Tools → PSS → Setup** — создаст `.asset` файлы для всех PSS скриптов. Без этого компоненты не появятся в сцене.

### 2. Базовый флоу
```
Trigger → Channel → Action(s)
```
- **Trigger** — определяет *когда* что-то происходит (клик, вход в зону, таймер…)
- **Channel** — диспетчер, принимает сигнал и вызывает список Action-ов
- **Action** — определяет *что* происходит (включить объект, запустить анимацию…)

### 3. Минимальный пример — кнопка включает объект

**Шаг 1.** Создай три GameObject: `Button`, `Channel`, `TargetObject`.

**Шаг 2.** На `Button`:
- Добавь компонент `PSS_OnInteract` → поле **Channel**: перетащи сюда `Channel`
- Объект становится интерактивным автоматически — отдельный VRC_Interactable не нужен

**Шаг 3.** На `Channel`:
- Добавь компонент `PSS_ChannelLocal`

**Шаг 4.** На `TargetObject`:
- Добавь компонент `PSS_SetActive` → **Targets**: `[TargetObject]`, **Operation**: `Toggle`

**Шаг 5.** Выдели `Channel` в инспекторе — появится список Actions.
Нажми **Rescan** или перетащи `PSS_SetActive` в список вручную.

Готово. Нажатие на Button → Channel → TargetObject включается/выключается.

### 4. Быстрый сетап через Wizard
**Tools → PSS → Quick Setup** — интерактивное окно:
выбираешь Trigger + Sync Mode + Action, нажимаешь **Create Chain**.
Wizard создаёт все объекты и связи автоматически.

Три Sync Mode:
- **Local** — событие только у локального игрока
- **Global** — событие для всех игроков (требует PSS_Network)
- **GlobalState** — синхронизированное состояние с корректным late-join

---

## Выбор режима синхронизации

Ключевой вопрос: **нужно ли событие другим игрокам, и важен ли late-join?**

```
Нужно только мне (локальному игроку)?
└── Local  →  Trigger → ChannelLocal → Action

Нужно всем сейчас, late-join не важен?
└── Global  →  Trigger → ChannelGlobal (+ PSS_Network) → Action
    Пример: открыть дверь, запустить эффект для всех одновременно

Нужно всем + лейт-джоинеры должны видеть актуальное состояние?
└── GlobalState (Bool)  →  StateSync хранит true/false
    Пример: свет включён/выключен — зашедший позже видит текущее состояние
    
└── GlobalState (Int/Float)  →  StateSync хранит число, DataSlot → ConditionalTrigger
    Пример: счётчик очков, уровень, пороговые события
```

### Почему ChannelGlobal не решает late-join

`PSS_ChannelGlobal` отправляет **событие** всем кто в инстансе прямо сейчас.
Если кнопку нажали 4 раза (тогл), игрок который зашёл после — не получит ничего
и увидит объект в состоянии по умолчанию, а не актуальном.

`PSS_StateSync` хранит **реальное текущее значение**, а не историю нажатий.
`OnDeserialization` доставляет его лейт-джоинеру автоматически при входе.

---

## Каналы

### PSS_ChannelLocal
Локальный диспетчер событий. Принимает сигнал от Trigger, вызывает все подключённые Actions.
Опции: задержка перед выполнением (`Delay`), случайный выбор одного Action из списка (`Randomize` + `Weight`).

**Пример:** кнопка открывает дверь с задержкой 0.5 сек
- GameObject `Door_Button`:
  - `PSS_OnInteract` → Channel: `DoorChannel`
- GameObject `DoorChannel`:
  - `PSS_ChannelLocal` → Delay: `0.5`
- GameObject `Door`:
  - `PSS_SetActive` → Targets: `[DoorOpen]`, Operation: `True`
  - (Channel Editor: перетащи PSS_SetActive в список DoorChannel)

---

### PSS_ChannelGlobal
Всё то же что ChannelLocal, но событие синхронизируется по сети — сработает у всех игроков одновременно. Требует `PSS_Network` в сцене.

> **Когда использовать:** мгновенная реакция всех игроков, где late-join не важен
> (спецэффект, одноразовое событие, синхронная анимация).
> Если нужен late-join — используй **GlobalState**.

**Сетап PSS_Network:**
Один GameObject с компонентом `PSS_Network` на всю сцену. Wizard создаёт его автоматически кнопкой «Создать PSS_Network в сцене» если не найден.

**Пример:** синхронная кнопка включает свет для всех
- GameObject `PSS_Network` (один на сцену): `PSS_Network`
- GameObject `Light_Button`: `PSS_OnInteract` → Channel: `LightChannel`
- GameObject `LightChannel`: `PSS_ChannelGlobal` → Network: `PSS_Network`
- GameObject `Light`: `PSS_SetActive` → Targets: `[LightObject]`, Operation: `Toggle`

---

## Триггеры

### PSS_OnInteract
Срабатывает когда локальный игрок кликает на объект. Нужен `VRC_Interactable` на том же GameObject для текста и радиуса взаимодействия.

**Пример:** нажать кнопку → активировать канал
- На кнопке: `PSS_OnInteract` → Channel: `MyChannel`
- Объект становится интерактивным автоматически при наличии метода `Interact()`

---

### PSS_OnEnterTrigger / PSS_OnExitTrigger
Срабатывает когда игрок входит / выходит из коллайдера. Коллайдер должен быть с `Is Trigger = true`. `Local Player Only` — реагировать только на локального игрока.

**Пример:** зона активирует ambient эффект при входе и выключает при выходе
- GameObject `Zone` (Box Collider, Is Trigger: ✓):
  - `PSS_OnEnterTrigger` → Local Player Only: ✓, Channel: `AmbientOnChannel`
  - `PSS_OnExitTrigger` → Local Player Only: ✓, Channel: `AmbientOffChannel`
- `AmbientOnChannel` → `PSS_SetActive` (Targets: [AmbientFX], Operation: True)
- `AmbientOffChannel` → `PSS_SetActive` (Targets: [AmbientFX], Operation: False)

---

### PSS_OnTimer
Срабатывает по таймеру. `Repeat` — повторять бесконечно. `Random Range` — случайный интервал между Interval и Max Interval. Можно остановить/возобновить через `Stop()` / `Resume()`.

**Пример:** мигающий огонь каждые 2–4 секунды
- На объекте: `PSS_OnTimer` → Interval: `2`, Repeat: ✓, Random Range: ✓, Max Interval: `4`, Channel: `FlickerChannel`
- `FlickerChannel` → `PSS_SetActive` (Targets: [FlameEffect], Operation: Toggle)

---

### PSS_OnSpawn
Срабатывает один раз при старте сцены (или при спауне объекта). Опционально — с задержкой.

**Пример:** проиграть intro анимацию через 1 сек после загрузки
- На объекте: `PSS_OnSpawn` → Delay: `1`, Channel: `IntroChannel`
- `IntroChannel` → `PSS_AnimationParam` (Animator: IntroAnim, Param: "Play", Type: Trigger)

---

### PSS_OnEnable / PSS_OnDisable
Срабатывает при включении / выключении GameObject. `Skip First Enable` — пропустить первое срабатывание при старте сцены.

**Пример:** звуковой эффект при включении объекта
- На объекте: `PSS_OnEnable` → Skip First: ✓, Channel: `SoundChannel`
- `SoundChannel` → `PSS_CallMethod` (Target: AudioUdonBehaviour, Event: "PlaySound")

---

### PSS_CustomTrigger
Именной триггер. Можно вызвать по прямой ссылке или по имени из Action `PSS_ActiveCustomTrigger`. Публичные методы `Activate()` / `ActivateWithPlayer(player)` — для вызова из любого Udon-скрипта.

**Пример:** несколько кнопок активируют один и тот же эффект по имени
- GameObject `EffectTrigger`:
  - `PSS_CustomTrigger` → Name: `"PlayEffect"`, Channel: `EffectChannel`
- Каждая кнопка:
  - `PSS_OnInteract` → Channel: `ButtonChannel_N`
  - `ButtonChannel_N` → `PSS_ActiveCustomTrigger` → Trigger: `EffectTrigger` (прямая ссылка)

---

### PSS_ConditionalTrigger
Срабатывает когда значение `PSS_DataSlot` удовлетворяет условию. Режимы: `OnChange` (автоматически при изменении слота), `OnUpdate` (каждый кадр), `Manual` (вызвать `EvaluateCondition()` вручную). `Fire Once` — не повторять пока условие не сбросится.

**Пример:** показать UI-сообщение когда счётчик достигнет 3
- GameObject `ScoreSlot`: `PSS_DataSlot` → Type: Int, Value: 0
- GameObject `ScoreCheck`:
  - `PSS_ConditionalTrigger` → Source: `ScoreSlot`, Condition: `GreaterOrEqual`, Int Threshold: `3`, Mode: OnChange, Fire Once: ✓, Channel: `ShowUIChannel`
- `ShowUIChannel` → `PSS_SetActive` (Targets: [WinUI], Operation: True)
- Кнопка → `PSS_SetDataSlot` → Target: `ScoreSlot`, Operation: Add, Int: 1

---

## Экшны

### PSS_SetActive
Включает / выключает / инвертирует `GameObject`. Поддерживает массив объектов.

| Operation | Что делает |
|-----------|-----------|
| True | SetActive(true) |
| False | SetActive(false) |
| Toggle | !activeSelf |

**Пример:** Toggle-кнопка для панели
- `PSS_SetActive` → Targets: `[Panel]`, Operation: `Toggle`

---

### PSS_AnimationParam
Устанавливает параметр аниматора. Поддерживает Trigger, Bool, Int, Float. Массив аниматоров.

**Пример:** открыть дверь через анимацию
- `PSS_AnimationParam` → Animators: `[DoorAnimator]`, Param Name: `"Open"`, Type: `Bool`, Bool Value: `true`

---

### PSS_CallMethod
Вызывает публичный метод на `UdonBehaviour`. Локально, у всех (`All`) или у owner (`Owner`).

**Пример:** запустить видеоплеер по кнопке
- `PSS_CallMethod` → Target: `VideoPlayerUdon`, Event: `"Play"`, Network: `Local`

---

### PSS_ActiveCustomTrigger
Активирует `PSS_CustomTrigger` — по прямой ссылке или по имени из заранее назначенного списка. `Pass Player Context` — передать triggeredPlayer в цепочку.

**Пример:** по таймеру случайно выбрать один из трёх эффектов
- Три канала с разными экшнами, каждый → `PSS_CustomTrigger` (Name: "Effect1/2/3")
- `PSS_OnTimer` → `RandomChannel` (Randomize: ✓, три CustomTrigger-экшна с разными Weight)

---

### PSS_SetDataSlot
Записывает значение в `PSS_DataSlot`. Операции: Set, Add, Subtract, Multiply, Divide.

**Пример:** кнопка увеличивает счётчик
- `PSS_SetDataSlot` → Target: `ScoreSlot`, Operation: `Add`, Int: `1`

---

### PSS_SetStateSync
Записывает значение в `PSS_StateSync`. Используется для изменения синхронизированного состояния.
При вызове: берёт ownership, обновляет значение локально, вызывает `RequestSerialization()` — все остальные получат `OnDeserialization`.

**Bool операции:** SetTrue, SetFalse, Toggle  
**Int/Float операции:** Set, Add, Subtract

> Используй `PSS_ChannelLocal` перед этим экшеном, **не** ChannelGlobal.
> Глобальность обеспечивает сам StateSync через `[UdonSynced]`.

---

## DataSlot

### PSS_DataSlot
Контейнер данных. Типы: Bool, Int, Float, Vector3, String. При изменении через Set/Add/etc. уведомляет подписанные `PSS_ConditionalTrigger` (режим OnChange).

Поля доступны другим Udon-скриптам напрямую: `slot.GetInt()`, `slot.SetInt(v)` и т.д.

**Пример:** хранить состояние двери (открыта/закрыта)
- GameObject `DoorState`: `PSS_DataSlot` → Type: Bool, Value: false
- При открытии: `PSS_SetDataSlot` → Target: DoorState, Bool: true
- При закрытии: `PSS_SetDataSlot` → Target: DoorState, Bool: false
- Проверка из другого скрипта: `doorState.GetBool()`

---

## Глобальное состояние (State Sync)

### PSS_StateSync
Синхронизированное состояние с корректным late-join. Хранит реальное значение (`bool`/`int`/`float`) — не воспроизводит историю нажатий.

Типы:
- **Bool** → при изменении стреляет в `channelOnTrue` или `channelOnFalse`
- **Int / Float** → при изменении пишет в `targetSlot` (DataSlot); реакция — через `PSS_ConditionalTrigger`

---

### GlobalState Bool — глобальный тогл с late-join

**Топология (создаётся Wizard автоматически):**
```
[Root]   OnInteract → ChannelLocal → SetStateSync(Toggle)
[_Sync]  StateSync(Bool)
           channelOnTrue  → SetActive(True)
           channelOnFalse → SetActive(False)
```

**Ручной сетап — глобальная кнопка света:**

Шаг 1. Создай объект `StateSync`:
- `PSS_StateSync` → valueType: `Bool`, applyOnStart: `false`
- `channelOnTrue`  → канал который включает свет
- `channelOnFalse` → канал который выключает свет

Шаг 2. Создай каналы и экшены:
- GameObject `TrueChannel`: `PSS_ChannelLocal` → `PSS_SetActive`(True)
- GameObject `FalseChannel`: `PSS_ChannelLocal` → `PSS_SetActive`(False)

Шаг 3. Кнопка:
- `PSS_OnInteract` → `PSS_ChannelLocal` → `PSS_SetStateSync`
- `PSS_SetStateSync` → target: `StateSync`, boolOp: `Toggle`

**Почему ChannelLocal, а не ChannelGlobal перед SetStateSync:**
SetStateSync сам вызывает `RequestSerialization()`. Если бы перед ним стоял ChannelGlobal,
несколько игроков могли бы одновременно взять ownership → race condition.
ChannelLocal гарантирует что ownership берёт один — тот кто нажал кнопку.

---

### GlobalState Int — синхронизированный счётчик с пороговым событием

**Топология (создаётся Wizard автоматически):**
```
[Root]   OnInteract → ChannelLocal → SetStateSync(Add, 1)
[_Sync]  StateSync(Int, targetSlot=DataSlot)
         DataSlot(Int)
         ConditionalTrigger(>= 5, OnChange, FireOnce) → ChannelLocal → Action
```

**Ручной сетап — кнопка увеличивает счётчик, при 5 показывает UI:**

Шаг 1. `PSS_DataSlot` → Type: Int, Value: 0

Шаг 2. `PSS_StateSync` → valueType: `Int`, targetSlot: DataSlot

Шаг 3. `PSS_ConditionalTrigger`:
- sourceSlot: DataSlot
- Condition: `GreaterOrEqual`, Threshold: `5`
- evalMode: `OnChange`, fireOnce: ✓
- channel → `PSS_SetActive`(WinUI, True)

Шаг 4. Кнопка:
- `PSS_OnInteract` → `PSS_ChannelLocal` → `PSS_SetStateSync`
- target: StateSync, numOp: `Add`, valueInt: `1`

**Почему не PSS_SetDataSlot напрямую:**
`PSS_SetDataSlot` пишет только локально. `PSS_SetStateSync` синхронизирует значение по сети
через `[UdonSynced]` — лейт-джоинер получит актуальный счётчик.

---

### GlobalState Float — синхронизированный параметр

Аналогично Int, но для вещественных значений. Типичный кейс — синхронизированный уровень громкости, яркость, позиция.

**Пример: кнопки регулируют синхронизированную яркость**
- `PSS_StateSync` → valueType: `Float`, targetSlot: `BrightnessSlot`
- Кнопка +: `PSS_SetStateSync` → numOp: `Add`, valueFloat: `0.1`
- Кнопка −: `PSS_SetStateSync` → numOp: `Subtract`, valueFloat: `0.1`
- `PSS_ConditionalTrigger` (GreaterOrEqual, 0.5) → включить яркий режим
- `PSS_ConditionalTrigger` (LessThan, 0.5) → включить тёмный режим

---

## LTCGI

### PSS_LtcgiControl
Управляет LTCGI из PSS-цепочки. Два режима:

| Mode | Что делает |
|------|-----------|
| Global | `_SetGlobalState(bool)` — вся система сразу |
| PerScreen | `_SetColor(idx, color/black)` — конкретные экраны |

Операции: `True` (включить), `False` (выключить), `Toggle` (инвертировать).

> ⚠️ Не используй `SetActive` на объектах с `LTCGI_Screen` — сломает рендер.
> В режиме PerScreen индексы кэшируются в `Start()` — `_GetIndex` вызывается один раз.

**Пример: кнопка включает/выключает весь LTCGI**
- На кнопке: `PSS_OnInteract` → Channel: `LightChannel`
- На канале: `PSS_ChannelLocal`
- `PSS_LtcgiControl` → Adapter: `LTCGI_UdonAdapter`, Mode: `Global`, Operation: `Toggle`

**Пример: зона активирует конкретный экран**
- `PSS_OnEnterTrigger` → `PSS_LtcgiControl` → Mode: `PerScreen`, Screens: `[ScreenObject]`, Operation: `True`
- `PSS_OnExitTrigger` → `PSS_LtcgiControl` → Mode: `PerScreen`, Screens: `[ScreenObject]`, Operation: `False`

**Пример: LTCGI с late-join (GlobalState Bool)**
- Wizard: Trigger=OnInteract, Sync=GlobalState, Type=Bool, Action=LtcgiControl
- Создаст: `SetStateSync(Toggle) → StateSync → channelOnTrue → LtcgiControl(True) / channelOnFalse → LtcgiControl(False)`
- Лейт-джоинер увидит актуальное состояние LTCGI автоматически

---

## Быстрые рецепты

### Дверь по кнопке (локальная)
```
[Button] PSS_OnInteract → ChannelLocal → PSS_AnimationParam (Open=true)
```

### Дверь по кнопке (для всех, без late-join)
```
[Button] PSS_OnInteract → ChannelGlobal (+ PSS_Network) → PSS_AnimationParam
```

### Свет-тогл (для всех + late-join safe)
```
[Button] PSS_OnInteract → ChannelLocal → SetStateSync(Toggle)
         StateSync(Bool) → channelOnTrue → SetActive(True)
                        → channelOnFalse → SetActive(False)
```

### Зона-триггер с эффектом
```
[Zone Collider] PSS_OnEnterTrigger → ChannelLocal → PSS_SetActive (Effect, True)
[Zone Collider] PSS_OnExitTrigger  → ChannelLocal → PSS_SetActive (Effect, False)
```

### Мигающий/периодический эффект
```
PSS_OnTimer (Interval, Repeat) → ChannelLocal → PSS_SetActive (Toggle)
```

### Счётчик с пороговым событием (локальный)
```
[Button] PSS_OnInteract → ChannelLocal → PSS_SetDataSlot (Add 1 → ScoreSlot)
PSS_ConditionalTrigger (ScoreSlot >= 3, OnChange) → ChannelLocal → PSS_SetActive (WinUI)
```

### Счётчик с пороговым событием (синхронный, late-join safe)
```
[Button] PSS_OnInteract → ChannelLocal → SetStateSync(Int, Add 1)
         StateSync(Int) → DataSlot → ConditionalTrigger(>= 3) → ChannelLocal → SetActive(WinUI)
```

### Intro при загрузке
```
PSS_OnSpawn (Delay: 1) → ChannelLocal → PSS_AnimationParam (Trigger: "Intro")
```

### Случайный выбор экшна
```
ChannelLocal (Randomize: ✓) → несколько Actions с разными Weight
```

### LTCGI тогл с late-join
```
[Button] PSS_OnInteract → ChannelLocal → SetStateSync(Bool, Toggle)
         StateSync(Bool) → channelOnTrue → LtcgiControl(True)
                        → channelOnFalse → LtcgiControl(False)
```

### Несколько кнопок → одно состояние
```
Каждая кнопка: PSS_OnInteract → ChannelLocal → SetStateSync(target: SharedStateSync)
SharedStateSync → channelOnTrue / channelOnFalse → Action
```
Ownership конфликтов нет — каждый берёт ownership в момент нажатия.

### Синхронизированный параметр (Float)
```
Кнопка+: PSS_OnInteract → ChannelLocal → SetStateSync(Float, Add 0.1)
Кнопка−: PSS_OnInteract → ChannelLocal → SetStateSync(Float, Subtract 0.1)
StateSync(Float) → DataSlot → ConditionalTrigger(условие) → Action
```
