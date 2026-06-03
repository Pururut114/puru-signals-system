# PSS v0.3 — Node UX Plan

> Цель: T23-style UX поверх существующей архитектуры PSS.
> Ядро не ломаем. Node — новый primary entry point, old workflows остаются.

---

## Концепция

Пользователь добавляет **один компонент** (`PSS_Node`) на любой объект.
В его инспекторе — всё: sync mode, triggers, actions. Как T23_Master.

Отдельные Trigger/Action компоненты остаются видимыми ниже (как в T23) — не подавляем.

---

## Архитектурные решения

### Option B — PSS_ChannelBase

Выделяем общий базовый класс чтобы TriggerBase мог держать ссылку на Local или Global:

```
PSS_ChannelBase (abstract)
  ├── PSS_ChannelLocal
  └── PSS_ChannelGlobal

PSS_TriggerBase._channel: PSS_ChannelLocal → PSS_ChannelBase
```

### SyncMode switch

При смене Local→Global в Node editor:
- Удаляем старый Channel компонент
- Добавляем новый
- Перевайриваем все Trigger._channel на объекте

### PSS_Network (Global)

Если Node.syncMode = Global и PSS_Network не найден в сцене —
показать предупреждение в инспекторе с кнопкой "Add to Scene".

### Actions visibility

Trigger/Action компоненты остаются видимыми ниже Node в инспекторе. Не подавляем.

---

## Файлы к созданию / изменению

### Новые

| Файл | Что |
|------|-----|
| `Runtime/Channel/PSS_ChannelBase.cs` | Abstract base: Trigger(), TriggerWithPlayer(), delay, randomize, _actions[] |
| `Runtime/PSS_Node.cs` | UdonSharpBehaviour: SyncMode enum, Node settings, Start() fallback wiring |
| `Editor/PSS_NodeEditor.cs` | CustomEditor: unified UI, + Trigger / + Action dropdowns, auto-wire |

### Изменения

| Файл | Что меняется |
|------|-------------|
| `Runtime/Channel/PSS_ChannelLocal.cs` | extends PSS_ChannelBase (was PSS_ModuleBase) |
| `Runtime/Channel/PSS_ChannelGlobal.cs` | extends PSS_ChannelBase |
| `Runtime/Base/PSS_TriggerBase.cs` | `_channel` тип: PSS_ChannelLocal → PSS_ChannelBase |
| `Editor/PSS_Wizard.cs` | Добавить PSS_Node в меню |
| `Editor/PSS_SpawnMenu.cs` | Добавить Node в spawn menu |
| `_gen_meta_assets.py` | Добавить PSS_Node в BEHAVIOURS |
| `package.json` | version → 0.3.0 |
| `CHANGELOG.md` | Запись v0.3.0 |

---

## Порядок работы

```
1. [x] PSS_ChannelBase — создан Runtime/Channel/PSS_ChannelBase.cs
2. [x] Обновить ChannelLocal (extends ChannelBase)
3. [x] Обновить PSS_TriggerBase._channel, ActionBase.channel, StateSync.channel → PSS_ChannelBase
4. [x] PSS_Node runtime — Runtime/PSS_Node.cs
5. [x] PSS_NodeEditor — Editor/PSS_NodeEditor.cs
6. [x] HierarchyMenu: Add Node item добавлен
7. [x] _gen_meta_assets.py → PSS_Node.asset + 4 .meta файла созданы
8. [ ] Тест в Unity → release v0.3.0
```

---

## Статус

**Реализовано, ожидает тест в Unity.** Версия поднята до 0.3.0 в package.json и CHANGELOG.
