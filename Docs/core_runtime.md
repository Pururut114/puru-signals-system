# PSS — Core Runtime API

> Документация базовых классов. Актуально для Фазы 1+.

---

## PSS_ModuleBase

`Runtime/Base/PSS_ModuleBase.cs`  
Базовый класс для всех модулей PSS. Наследует `UdonSharpBehaviour`.

```csharp
public abstract class PSS_ModuleBase : UdonSharpBehaviour
{
    [HideInInspector] public string moduleName;  // отображается в инспекторе
}
```

---

## PSS_TriggerBase

`Runtime/Base/PSS_TriggerBase.cs`  
Наследуй чтобы создать Trigger.

```csharp
public abstract class PSS_TriggerBase : PSS_ModuleBase
{
    public PSS_ChannelLocal channel;  // drag-and-drop в инспекторе
}
```

**Методы для вызова в наследнике:**

| Метод | Описание |
|-------|---------|
| `Fire()` | Активировать канал без player-контекста |
| `FireWithPlayer(VRCPlayerApi player)` | Активировать с player-контекстом (будет в `channel.triggeredPlayer`) |

**Пример наследника:**
```csharp
[AddComponentMenu("PSS/Triggers/OnInteract")]
public class PSS_OnInteract : PSS_TriggerBase
{
    public override void Interact()
    {
        FireWithPlayer(Networking.LocalPlayer);
    }
}
```

---

## PSS_ActionBase

`Runtime/Base/PSS_ActionBase.cs`  
Наследуй чтобы создать Action. Переопределяй `OnExecute()`.

```csharp
public abstract class PSS_ActionBase : PSS_ModuleBase
{
    [HideInInspector] public PSS_ChannelLocal channel;  // ссылка на канал (read-only)
    [HideInInspector] public int priority = 0;           // меньше = выполняется раньше
    [HideInInspector] public float weight = 1f;          // вес при randomize
}
```

**Поля (заполняет PSS_ChannelEditor, не трогать вручную):**
- `channel` — ссылка на канал, которому принадлежит Action. Доступ к `channel.triggeredPlayer` внутри `OnExecute()`
- `priority` — порядок выполнения (сортировка в _actions[])
- `weight` — вес для weighted-random dispatch

**Метод для переопределения:**
```csharp
protected abstract void OnExecute();
// Вся логика Action — здесь
```

**Пример наследника:**
```csharp
[AddComponentMenu("PSS/Actions/SetActive")]
public class PSS_SetActive : PSS_ActionBase
{
    public GameObject[] targets;
    public bool value = true;

    protected override void OnExecute()
    {
        foreach (var t in targets)
            if (t != null) t.SetActive(value);
    }
}
```

---

## PSS_ChannelLocal

`Runtime/Channel/PSS_ChannelLocal.cs`  
Диспетчер событий. Принимает сигнал от Triggers, передаёт Actions.

**Инспектор-поля:**

| Поле | Тип | Описание |
|------|-----|---------|
| `delay` | float | Задержка (сек) перед dispatch |
| `randomize` | bool | Weighted-random: выбрать один Action вместо всех |

**Скрытые поля (редактор):**

| Поле | Тип | Кто заполняет |
|------|-----|--------------|
| `_actions[]` | `PSS_ActionBase[]` | PSS_ChannelEditor (автоматически) |
| `triggeredPlayer` | `VRCPlayerApi` | Заполняется при каждом событии |

**Public API:**

| Метод | Описание |
|-------|---------|
| `Trigger()` | Активировать (сбрасывает triggeredPlayer) |
| `TriggerWithPlayer(VRCPlayerApi)` | Активировать с player-контекстом |
| `_Fire()` | Внутренний метод dispatch (нужен для delayed call, вызывать напрямую не нужно) |

**Порядок dispatch:**
1. Если `delay > 0` → `SendCustomEventDelayedSeconds("_Fire", delay)`
2. Иначе → `_Fire()` сразу
3. В `_Fire()`:
   - `randomize = false` → вызвать `Execute()` на каждом Action по порядку
   - `randomize = true` → weighted-random по `weight`, вызвать один Action

**Как Channel находит свои Actions:**  
Редактор (PSS_ChannelEditor) сканирует сцену при каждом изменении и собирает все `PSS_ActionBase` компоненты у которых `channel == this`. Сортирует по `priority`. Записывает в `_actions[]`. Это происходит в Editor-time, в runtime массив уже готов.

---

## Жизненный цикл события

```
1. Игровое событие (коллайдер, кнопка, таймер...)
2. PSS_TriggerBase.Fire() или FireWithPlayer()
3. PSS_ChannelLocal.Trigger() / TriggerWithPlayer()
   → сохраняет triggeredPlayer
   → если delay > 0: откладывает _Fire()
4. PSS_ChannelLocal._Fire()
   → если randomize: weighted random по weight
   → иначе: foreach _actions[] → action.Execute()
5. PSS_ActionBase.Execute()
   → вызывает OnExecute() (переопределён в конкретном классе)
6. Конкретный Action выполняет логику
   → может читать channel.triggeredPlayer если нужен контекст игрока
```

---

## Ограничения UdonSharp (важно для разработки модулей)

- **Нет generics в runtime** — всё через конкретные типы
- **Нет cross-script interface вызовов** — только через прямые ссылки или `SendCustomEvent`
- **`Execute()` вызывается напрямую** (`_actions[i].Execute()`) — UdonSharp компилирует это в cross-program event dispatch
- **Abstract классы** — PSS_ModuleBase, PSS_TriggerBase, PSS_ActionBase абстрактны. Компилируются в program asset только конкретные наследники
- **`[HideInInspector]`** на полях которые заполняет редактор — UdonSharp сериализует их, но они не видны в дефолтном инспекторе
