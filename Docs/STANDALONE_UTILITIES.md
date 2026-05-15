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

## Добавить новую утилиту

Краткий чеклист — полная инструкция в `Docs/ADDING_MODULES.md`:

- [ ] Файл в `Modules/Standalone Utilities/<Category>/PSS_<Name>.cs`
- [ ] Наследование от `UdonSharpBehaviour`, атрибут `[UdonBehaviourSyncMode]`
- [ ] `[AddComponentMenu("PSS/Standalone Utilities/<Category>/PSS_<Name> [Utility]")]`
- [ ] `[MenuItem]` в `Editor/PSS_SpawnMenu.cs` — создаёт объект с правильными defaults
- [ ] Строка в `Docs/modules.md`
- [ ] Секция в этом файле
