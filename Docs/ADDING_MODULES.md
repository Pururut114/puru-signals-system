# Как добавить новый модуль в PSS

> **Этот файл — для новых чатов.** Читай его если нужно добавить Trigger или Action не зная истории проекта.
> Архитектура целиком — в `ARCHITECTURE.md`. Что уже сделано — в `PLAN.md`.

---

## Быстрый старт

### Добавить Trigger

1. Создать файл: `Modules/<PackName>/Triggers/PSS_<Name>.cs`
2. Наследовать от `PSS_TriggerBase`
3. Поставить атрибут `[PSS_Module("Trigger/<Name>")]`
4. Поставить `[AddComponentMenu("PSS/Triggers/PSS_<Name> [Trigger]")]`
5. Описать поля через `[PSS_Field(...)]`
6. В нужном событии вызвать `Fire()` (или `FireWithPlayer(player)`)
7. **Запустить `_gen_meta_assets.py`** — сгенерирует `.asset` и `.meta` файлы рядом с `.cs`
8. Добавить строку в `Docs/modules.md` (реестр)
9. Добавить секцию в `Docs/CHEATSHEET.md` (пример сетапа)
10. Добавить в `Editor/PSS_Wizard.cs` — новый вариант в `TriggerKind`, `TriggerType()`, `DrawTriggerFields()`, `ApplyTriggerFields()`

### Добавить Standalone Utility

1. Создать файл: `Modules/Standalone Utilities/<Category>/PSS_<Name>.cs`
2. Наследовать от `UdonSharpBehaviour` напрямую (не от PSS_ActionBase/TriggerBase)
3. Поставить `[UdonBehaviourSyncMode(BehaviourSyncMode.None)]`
4. Поставить `[AddComponentMenu("PSS/Standalone Utilities/<Category>/PSS_<Name> [Utility]")]`
5. Поля — стандартные `[Header]` / `[Tooltip]`, без `[PSS_Field]`
6. **Запустить `_gen_meta_assets.py`** — сгенерирует `.asset` и `.meta` файлы рядом с `.cs`
7. Добавить строку в `Docs/modules.md` → секция Standalone Utilities
8. Добавить секцию в `Docs/STANDALONE_UTILITIES.md`
9. Добавить `[MenuItem("Tools/PSS/Spawn/<Category>/<Name>")]` в `Editor/PSS_SpawnMenu.cs`

> Wizard (`PSS_Wizard.cs`) не трогать — standalone утилиты живут отдельно.

---

### Добавить Action

1. Создать файл: `Modules/<PackName>/Actions/PSS_<Name>.cs`
2. Наследовать от `PSS_ActionBase`
3. Поставить атрибут `[PSS_Module("Action/<Name>")]`
4. Поставить `[AddComponentMenu("PSS/Actions/PSS_<Name> [Action]")]`
5. Описать поля через `[PSS_Field(...)]`
6. Переопределить `protected override void OnExecute()`
7. **Запустить `_gen_meta_assets.py`** — сгенерирует `.asset` и `.meta` файлы рядом с `.cs`
8. Добавить строку в `Docs/modules.md` (реестр)
9. Добавить секцию в `Docs/CHEATSHEET.md` (пример сетапа)
10. Добавить в `Editor/PSS_Wizard.cs` — новый вариант в `ActionKind`, `ActionType()`, `DrawActionFields()`, `ApplyActionFields()`

---

## Полный шаблон Trigger

```csharp
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using PuruSignals;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Trigger/MyTrigger")]
    [AddComponentMenu("PSS/Triggers/PSS_MyTrigger [Trigger]")]
    public class PSS_MyTrigger : PSS_TriggerBase
    {
        // Поля с атрибутами — см. раздел "Атрибуты PSS_Field" ниже
        [PSS_Field("My Float")]
        public float myFloat = 1f;

        [PSS_Field("My Bool")]
        public bool myBool = false;

        // Start, если нужен
        private void Start()
        {
            // инициализация
        }

        // Вызывается когда нужно активировать канал
        private void MyEvent()
        {
            Fire();
            // или: FireWithPlayer(somePlayer);
        }
    }
}
```

---

## Полный шаблон Action

```csharp
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using PuruSignals;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Action/MyAction")]
    [AddComponentMenu("PSS/Actions/PSS_MyAction [Action]")]
    public class PSS_MyAction : PSS_ActionBase
    {
        [PSS_Field("Targets", isList: true)]
        public GameObject[] targets;

        [PSS_Field("Value")]
        public bool value = true;

        protected override void OnExecute()
        {
            foreach (var t in targets)
            {
                if (t != null)
                    t.SetActive(value);
            }
        }
    }
}
```

---

## Атрибуты PSS_Field

```csharp
// Простые типы
[PSS_Field("Label")]                          // auto-detect type
[PSS_Field("Speed", min: 0f)]                // float с минимумом
[PSS_Field("Speed", min: 0f, max: 10f)]      // float с диапазоном
[PSS_Field("Targets", isList: true)]         // ReorderableList
[PSS_Field("Value", showIf: "myBool")]       // показывать только если myBool == true

// DataSlot toggle — в инспекторе кнопка переключает между константой и DataSlot
[PSS_Field("Speed", canUseDataSlot: true)]
public float speed = 1f;
public PSS_DataSlot speedSlot;              // companion поле, имя = fieldName + "Slot"
```

---

## API PSS_TriggerBase

```csharp
// Доступно в любом наследнике:

// Активировать канал (без player-контекста)
protected void Fire();

// Активировать канал с player-контекстом (нужен для ConditionalTrigger с TriggeredPlayer)
protected void FireWithPlayer(VRCPlayerApi player);

// Прямая ссылка на канал (можно читать, менять не надо)
protected PSS_ChannelLocal _channel;
```

---

## API PSS_ActionBase

```csharp
// Переопределить обязательно:
protected abstract void OnExecute();

// Доступно в OnExecute:
channel.triggeredPlayer   // VRCPlayerApi, если был FireWithPlayer

// Поля (заполняются Channel'ом автоматически, не трогать):
[HideInInspector] public PSS_ChannelLocal channel;
[HideInInspector] public int priority;
[HideInInspector] public float weight;
```

---

## API PSS_ChannelLocal

```csharp
// Вызывается триггерами:
public void Trigger();
public void TriggerWithPlayer(VRCPlayerApi player);

// Данные текущего события (читать в Actions):
public VRCPlayerApi triggeredPlayer;

// Настройки (задаются в инспекторе):
public bool randomize;
public float delay;
```

---

## API PSS_DataSlot

```csharp
// Типы: 0=bool, 1=int, 2=float, 3=vec3, 4=string
public int valueType;

// Чтение:
public bool    GetBool();
public int     GetInt();
public float   GetFloat();
public Vector3 GetVec3();
public string  GetString();

// Запись:
public void SetBool(bool v);
public void SetInt(int v);
public void SetFloat(float v);
public void SetVec3(Vector3 v);
public void SetString(string v);
```

---

## Структура пакета модулей

Каждый пакет (Physics, Player, Audio...) — независимая папка в `Modules/`:

```
Modules/
└── MyPack/
    ├── Triggers/
    │   └── PSS_OnMyEvent.cs
    └── Actions/
        ├── PSS_DoSomething.cs
        └── PSS_DoSomethingElse.cs
```

Пакет не требует изменений в ядре. Просто добавь папку с файлами.

> **Если создаёшь НОВУЮ сборку (новый `.asmdef` файл):** рядом с ним нужно создать `UdonSharpAssemblyDefinition` ассет — иначе UdonSharp не распознает сборку как U# и все скрипты получат ошибку `"does not belong to a U# assembly"`.
> В Unity: выдели `.asmdef` → right-click → **Create → U# Assembly Definition**.
> Добавь созданный `.asset` и `.asset.meta` в git.

> **Если сборка имеет `defineConstraints`** (условная зависимость, как LTCGI/ProTV):
> 1. `UdonSharpAssemblyDefinition` — добавить в репо (безвредна когда assembly не компилируется).
> 2. `UdonSharpProgramAsset` (`.asset`) файлы для скриптов этой сборки — **НЕ** включать в репо. Когда define отсутствует — assembly не компилируется, type = null, UdonSharp зацикливается на ошибках.
> 3. В `PSS_AutoSetup.cs` → `SyncDefines()` добавить одну строку:
>    ```csharp
>    changed |= SyncDefine(defines, "PSS_MYTOOL_INSTALLED", IsAssemblyLoaded("MyTool.AssemblyName"));
>    ```
>    Имя assembly берётся из `.asmdef` файла пакета зависимости (поле `"name"`).
> 4. В `_validate_release.py` добавить папку в `SKIP_ASSET` и `CONDITIONAL_DIRS`.
>
> После этого: установил пакет → `PSS_AutoSetup` автоматически добавляет define, перекомпилирует, создаёт program assets. Удалил → убирает define. Пользователь ничего не делает вручную.

---

## UdonSharp ограничения

- **`enum` внутри класса** — не поддерживается. Объявлять на уровне `namespace`, перед классом:
  ```csharp
  namespace PuruSignals
  {
      public enum MyOp { A, B, C }   // ← здесь, не внутри класса

      public class PSS_MyAction : PSS_ActionBase { ... }
  }
  ```
- **`OnEnable` / `OnDisable`** — magic methods, не `virtual`. Писать `private void OnEnable()` без `override`.
- **`[UdonBehaviourSyncMode]`** — обязателен на каждом конкретном классе. Без него UdonSharp не будет компилировать скрипт.
- **`System.Array.Copy`** — не поддерживается. Заменять на for-loop.

---

## Checklist перед завершением модуля

- [ ] Компилируется в UdonSharp без ошибок
- [ ] Атрибуты `[PSS_Module]` и `[AddComponentMenu]` проставлены
- [ ] Все поля с `[PSS_Field]` корректно отображаются в инспекторе
- [ ] `OnExecute` (для Action) или `Fire()` (для Trigger) вызывается в правильный момент
- [ ] `_gen_meta_assets.py` запущен — `.asset` и `.meta` файлы сгенерированы и лежат рядом с `.cs`
- [ ] Строка добавлена в `Docs/modules.md`
- [ ] Секция добавлена в `Docs/CHEATSHEET.md`
- [ ] **Trigger / Action:** добавлен в `Editor/PSS_Wizard.cs` (ActionKind/TriggerKind + все 3 switch-а)
- [ ] **Standalone Utility:** добавлен в `Editor/PSS_SpawnMenu.cs` (`Tools > PSS > Spawn > ...`)
- [ ] `package.json` version обновлён, `CHANGELOG.md` заполнен
