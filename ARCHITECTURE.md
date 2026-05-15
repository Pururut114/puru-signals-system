# Puru Signals System — Architecture

> Справочный документ. Содержит: концепцию, ключевые решения, глоссарий, API базовых классов.
> Для чеклиста задач — `PLAN.md`. Для добавления модулей — `Docs/ADDING_MODULES.md`.

---

## Концепция

**PSS (Puru Signals System)** — event-driven фреймворк для VRChat миров на UdonSharp.  
Позволяет строить интерактивные системы без написания Udon-кода через визуальную связку **Trigger → Channel → Action**.

Вдохновлён Trigger2to3, но с переработанной архитектурой:
- Прямые ссылки вместо magic-number groupID
- Без лимита на количество сетевых каналов
- Attribute-driven editor — добавление нового модуля = один `.cs` файл
- Раздельные модульные пакеты, расширяемые без изменения ядра

---

## Глоссарий

| Термин | Описание |
|--------|---------|
| **Channel** | Диспетчер событий. Принимает сигнал от Trigger(s), вызывает все подписанные Actions в порядке priority. Аналог Broadcast в T23. |
| **Trigger** | Детектирует событие (взаимодействие, коллайдер, таймер...) и вызывает Channel. |
| **Action** | Выполняет логику (включить объект, запустить анимацию...) в ответ на Channel. |
| **DataSlot** | Контейнер данных. Хранит значение (bool/int/float/vec3/string). Может использоваться Actions для чтения/записи динамических значений. |
| **Node** | Опциональный контейнер-группировщик. Содержит Channel + его Triggers + Actions на одном GameObject. Удобен для компактных сетапов. |
| **PSS_Network** | Singleton в сцене. Роутит все сетевые события для ChannelGlobal. Один на сцену. |

---

## Схема связей

```
[Trigger] ──→ [Channel] ──→ [Action]
                  │
                  └──→ [Action]
                  └──→ [Action]

[Trigger] ──→ [Channel]   (несколько Triggers на один Channel — ок)
[Trigger] ──→ [Channel]

[DataSlot] ←──→ [Action]  (Action читает или пишет DataSlot)
[DataSlot] ←── [ConditionalTrigger]  (Trigger читает DataSlot)
```

---

## Ключевые архитектурные решения

### 1. Прямые ссылки вместо groupID
Trigger хранит `[SerializeField] PSS_ChannelLocal channel` — прямую ссылку на Channel.  
Нет магических чисел. Drag-and-drop. Работает с `FindObjectsOfType` в Editor, ничего не ищется по int.

### 2. Сеть без лимита групп
**Проблема T23:** `SendCustomNetworkEvent` требует имя метода → пришлось захардкодить `RecieveNetworkFire0..9`.

**PSS решение:** `PSS_Network` — один UdonSharpBehaviour в сцене с синкованным `int _pendingId` и `bool _pendingTick`.
- Все `PSS_ChannelGlobal` регистрируются в PSS_Network при `Start()`, получают числовой `networkId`
- Когда ChannelGlobal получает сигнал → пишет свой id в `PSS_Network._pendingId`, флипает `_pendingTick`, вызывает `RequestSerialization()`
- Все клиенты получают `OnDeserialization` → PSS_Network вызывает `RouteEvent(pendingId)` → находит нужный Channel → `Channel.Fire()`

Результат: unlimited global channels, одна точка сериализации.

### 3. Attribute-driven editor
Вместо отдельного `*Editor.cs` для каждого модуля — атрибуты на полях класса:

```csharp
[PSS_Module("Trigger/OnTimer")]          // регистрирует в меню
public class PSS_OnTimer : PSS_TriggerBase
{
    [PSS_Field("Interval", min: 0.1f)]   // float поле с min
    public float interval = 1f;

    [PSS_Field("Repeat")]                // bool поле
    public bool repeat = true;
}
```

`PSS_GenericEditor` (один CustomEditor для всех наследников `PSS_ModuleBase`) читает атрибуты через reflection и рисует инспектор.

**Типы PSS_Field:**
- `float` → FloatField (с optional min/max)
- `int` → IntField
- `bool` → Toggle
- `string` → TextField
- `GameObject` → ObjectField
- `UdonSharpBehaviour` → ObjectField (с типовой фильтрацией)
- `GameObject[]` → ReorderableList (помечается атрибутом `isList: true`)
- `enum` → EnumPopup
- Поле может иметь `toggle: "fieldName"` — добавляет кнопку-переключатель для "константа vs DataSlot"

### 4. Actions отключены по умолчанию
Как в T23 — Actions находятся в `enabled = false` между вызовами. Channel вызывает `SendCustomEvent("Execute")` на нужном Action. Это экономит Update-циклы.

### 5. Priority
Чем **меньше** значение priority у Action — тем **раньше** оно выполняется.  
Channel сортирует _actions[] по priority при Start().

### 6. Randomization
Channel поддерживает `randomize` режим:
- У каждого Action есть `weight` (float, default 1.0)
- При `randomize = true` Channel генерирует случайное число и выбирает Action по weighted-random
- Seed для сетевых каналов хранится в PSS_Network → все клиенты получают одинаковый результат

---

## Базовые классы — API

### PSS_ModuleBase
```csharp
// Runtime/Base/PSS_ModuleBase.cs
// Базовый класс для всех модулей PSS
public abstract class PSS_ModuleBase : UdonSharpBehaviour
{
    // Имя модуля (отображается в инспекторе)
    [HideInInspector] public string moduleName;
}
```

### PSS_TriggerBase
```csharp
// Runtime/Base/PSS_TriggerBase.cs
public abstract class PSS_TriggerBase : PSS_ModuleBase
{
    // Канал, который этот триггер активирует
    [SerializeField] protected PSS_ChannelLocal _channel;

    // Вызвать для активации канала (из любого наследника)
    protected void Fire();
    protected void FireWithPlayer(VRCPlayerApi player);
}
```

### PSS_ActionBase
```csharp
// Runtime/Base/PSS_ActionBase.cs
public abstract class PSS_ActionBase : PSS_ModuleBase
{
    [HideInInspector] public PSS_ChannelLocal channel;   // канал, на который подписан
    [HideInInspector] public int priority;               // порядок выполнения
    [HideInInspector] public float weight;               // вес для randomize

    // Вызывается Channel'ом
    public void Execute();

    // Переопределить в наследнике
    protected abstract void OnExecute();
}
```

### PSS_ChannelLocal
```csharp
// Runtime/Channel/PSS_ChannelLocal.cs
public class PSS_ChannelLocal : PSS_ModuleBase
{
    public bool randomize;
    public float delay;

    [HideInInspector] public PSS_ActionBase[] _actions;  // заполняется в Start()
    [HideInInspector] public VRCPlayerApi triggeredPlayer;

    // Вызывается триггером
    public void Trigger();
    public void TriggerWithPlayer(VRCPlayerApi player);

    // Внутренний dispatch
    private void Fire();
}
```

### PSS_DataSlot
```csharp
// Runtime/Data/PSS_DataSlot.cs
public class PSS_DataSlot : PSS_ModuleBase
{
    public int valueType;  // 0=bool, 1=int, 2=float, 3=vec3, 4=string

    public bool   valueBool;
    public int    valueInt;
    public float  valueFloat;
    public Vector3 valueVec3;
    public string  valueString;

    // Helpers
    public bool   GetBool();
    public int    GetInt();
    public float  GetFloat();
    public Vector3 GetVec3();
    public string  GetString();

    public void SetBool(bool v);
    public void SetInt(int v);
    public void SetFloat(float v);
    public void SetVec3(Vector3 v);
    public void SetString(string v);
}
```

---

## Структура модуля (паттерн)

Каждый модуль — один `.cs` файл, содержащий:
1. Runtime-класс (наследник TriggerBase или ActionBase)
2. Опциональный Editor override в `#if UNITY_EDITOR` блоке (только если нужна нестандартная логика инспектора)

```csharp
// Modules/Core/Triggers/PSS_OnTimer.cs

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    [PSS_Module("Trigger/OnTimer")]
    [AddComponentMenu("PSS/Triggers/OnTimer")]
    public class PSS_OnTimer : PSS_TriggerBase
    {
        [PSS_Field("Interval (sec)", min: 0.01f)]
        public float interval = 1f;

        [PSS_Field("Repeat")]
        public bool repeat = true;

        [PSS_Field("Random Range")]
        public bool randomRange = false;

        [PSS_Field("Max Interval", min: 0.01f, showIf: "randomRange")]
        public float intervalMax = 2f;

        private void Start()
        {
            SendCustomEventDelayedSeconds(nameof(_Tick), GetDelay());
        }

        public void _Tick()
        {
            Fire();
            if (repeat)
                SendCustomEventDelayedSeconds(nameof(_Tick), GetDelay());
        }

        private float GetDelay()
        {
            if (!randomRange) return interval;
            return Random.Range(interval, intervalMax);
        }
    }
}
```

---

## Сравнение с Trigger2to3

| Аспект | Trigger2to3 | PSS |
|--------|-------------|-----|
| Связь Trigger↔Broadcast | groupID (int) | прямая ссылка |
| Max global каналов | 10 | unlimited |
| Добавить новый Action | ~2 файла (Runtime + Editor) | 1 файл |
| Editor | per-module Editor class | attribute-driven |
| Данные | PropertyBox (монолит) | PSS_DataSlot (легковес) |
| Сеть | hardcoded event names | PSS_Network router |
| Модульность | монорепо | независимые пакеты |

---

## Зависимости

- **Unity** 2022.3 LTS+
- **VRChat SDK** 3.x (Worlds)
- **UdonSharp** 1.x
- Опционально: **AudioLink**, **LTCGI** (в соответствующих модулях)

---

## Известные ограничения UdonSharp (учитываем при разработке)

- Нет generics в runtime
- Нет interfaces для cross-script вызовов (используем `SendCustomEvent` / прямые ссылки)
- `SendCustomNetworkEvent` требует строковое имя метода (поэтому PSS_Network как роутер)
- Reflection недоступна в runtime (только в Editor)
- Все `[SerializeField]` поля должны быть конкретных типов (не `UdonSharpBehaviour` как базовый — нужен конкретный тип или `Component`)
