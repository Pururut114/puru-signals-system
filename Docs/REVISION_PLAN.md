# PSS v0.3 — Plan Revision: Script Compatibility Audit

Цель: пройтись по всем user flow, верифицировать code path, зафиксировать риски.

---

## User Flows

### Flow 1 — Local Interact → SetActive
**Setup:** PSS_Node(Local) + PSS_OnInteract + PSS_SetActive(Toggle)

**Code path:**
```
VRC Interact() → Fire() → channel.Trigger()
  → PSS_ChannelLocal.Trigger() [inherited from Base]
    → _Dispatch() → _Fire()
      → _FireAll() → action[0].Execute() → OnExecute()
        → target.SetActive(!target.activeSelf)
```

**Risks:**
- `_actions[]` пустой → `_Fire()` выходит сразу. Root cause — RescanActions timing при добавлении Action через Node.
- `_Dispatch()` private в Base. C# inherited private недоступен в subclass напрямую, но вызов идёт из унаследованного `Trigger()` — должно компилироваться корректно. **Нужна верификация в Client Sim.**

---

### Flow 2 — Local Zone Enter → SetActive
**Setup:** PSS_Node(Local) + PSS_OnEnterTrigger + BoxCollider(IsTrigger) + PSS_SetActive

**Code path:**
```
OnPlayerTriggerEnter(player) → [localPlayerOnly check] → FireWithPlayer(player)
  → channel.TriggerWithPlayer(player) → triggeredPlayer = player → _Dispatch() → ...
```

**Risks:**
- BoxCollider должен быть на том же GameObject или иметь правильный Layer. VRChat `OnPlayerTriggerEnter` работает только с collider на объекте с UdonSharpBehaviour.
- `localPlayerOnly = true` по дефолту — правильно для Local сетапа.

---

### Flow 3 — OnEnable при старте
**Setup:** PSS_Node + PSS_OnEnable + PSS_SetActive

**Code path:**
```
GameObject.OnEnable() → [skipFirst check] → Fire() → channel.Trigger() → ...
```

**Risks:**
- `skipFirst = false` по умолчанию → ВСЕГДА срабатывает при старте сцены. Для "только при явном Enable" — ставить `skipFirst = true`.
- `_firstDone` — приватное поле, теряется при domain reload в Editor. В runtime работает нормально.
- **Вероятный баг в тесте:** куб исчезает (OnEnable сработал) → re-enter в зону не работает потому что триггер `OnEnterTrigger` отсутствовал, либо `_actions` на нём пустой.

---

### Flow 4 — OnTimer loop
**Setup:** PSS_Node + PSS_OnTimer(interval=5) + любой Action

**Code path:**
```
Start() → SendCustomEventDelayedSeconds("_Tick", delay/interval)
  → _Tick() → Fire() → channel.Trigger() → ...
  → if repeat → SendCustomEventDelayedSeconds("_Tick", interval)
```

**Risks:**
- Чистый, без рисков по архитектуре.
- Если `_actions` пустой — тихо падает на каждый тик. Нужна верификация `_actions`.

---

### Flow 5 — Global Interact → SetActive (все игроки видят)
**Setup:** PSS_Node(Global) + PSS_Network + PSS_OnInteract + PSS_SetActive

**Code path:**
```
Interact() → FireWithPlayer(local)
  → PSS_ChannelGlobal.TriggerWithPlayer(player) [virtual override]  ← КРИТИЧНО
    → _SendToNetwork() → network.SendGlobalEvent(id, seed)
      → PSS_Network.RequestSerialization()
      → [Owner] _ReceiveNetworkFire(seed) локально → _Fire() → actions
      → [Clients] OnDeserialization() → _ReceiveNetworkFire(seed) → _Fire() → actions
```

**Risks:**
- **Виртуальный dispatch:** `channel` typed как `PSS_ChannelLocal`, реальный объект — `PSS_ChannelGlobal`. UdonSharp cross-behaviour вызов идёт через `SendCustomEvent` на реальный UdonBehaviour → должен резолвиться в `PSS_ChannelGlobal.Trigger()`. **Нужна верификация в Client Sim.**
- **`_actions` на клиентах:** массив не синхронизируется по сети, берётся из сохранённой сцены. Если RescanActions не отработал до билда — у всех клиентов пустой `_actions`.
- PSS_Network должен быть в сцене и канал зарегистрирован в `Start()`. Если Network добавляется после билда — не работает.
- `_pendingTick` flip-flop нужен чтобы повторный триггер одного канала пробивался через OnDeserialization даже если `_pendingId` не изменился.

---

### Flow 6 — Global + Buffer for Late Join
**Setup:** Flow 5 + `bufferForLateJoin = true` на PSS_ChannelGlobal

**Code path:**
```
New player joins → PSS_Network.OnPlayerJoined() → [IsOwner check]
  → RequestSerialization() → новый клиент: OnDeserialization() → _ReceiveNetworkFire(seed) → _Fire()
```

**Risks:**
- Буфер воспроизводит **последнее событие**, не текущее состояние. Для статичного состояния (toggle вкл/выкл) нужен PSS_StateSync.
- Если с момента события прошло время и объект уже был ещё раз toggled — поздний игрок получит неверное состояние.
- Для правильного state-based buffering → Flow 7.

---

### Flow 7 — GlobalStateSync (правильный state для опоздавших)
**Setup:** PSS_StateSync(Manual sync) + PSS_SetStateSync action + channelOnTrue/channelOnFalse

**Code path:**
```
trigger → SetStateSync.Execute() → target.Toggle()
  → _syncBool = !_syncBool → _ApplyState() → channelOnTrue/False.Trigger() → actions
  → _Sync() → Networking.SetOwner() → RequestSerialization()
  → [Late join] OnDeserialization() → _ApplyState() → correct channel fires
```

**Risks:**
- `channelOnTrue` / `channelOnFalse` typed как `PSS_ChannelLocal` — та же virtual dispatch проблема.
- `applyOnStart = false` по умолчанию — поздние игроки не получат ничего пока owner не сделает action. Для начального состояния: `applyOnStart = true` на owner-side.
- Цикл: StateSync.Trigger() → Channel → SetStateSync → StateSync.Toggle() → бесконечность. Нужно следить за архитектурой.

---

### Flow 8 — CustomTrigger chain (A → B)
**Setup:** Node A: OnInteract → PSS_ActiveCustomTrigger(target=Node B's CustomTrigger) / Node B: PSS_CustomTrigger → SetActive

**Code path:**
```
NodeA: Interact() → channel_A.Trigger() → _Fire() → ActiveCustomTrigger.Execute()
  → targetTrigger.Activate() → Fire() [on Node B's trigger]
    → channel_B.Trigger() → _Fire() → NodeB's actions
```

**Risks:**
- `passPlayer = true`: читает `channel.triggeredPlayer` из **Action's channel** (channel_A), не из channel_B. Нужно убедиться что channel_A — это канал NodeA с правильным triggeredPlayer.
- Name-based поиск по `candidates[]` — requires manual list, нет auto-scan.

---

### Flow 9 — Delay dispatch
**Setup:** Channel с `delay > 0` + любой Trigger

**Code path:**
```
Trigger() → _Dispatch() → SendCustomEventDelayedSeconds("_Fire", delay) → [delay sec] → _Fire()
```

**Risks:**
- `nameof(_Fire)` в abstract class PSS_ChannelBase → строка `"_Fire"`. UdonSharp должен сохранить точное имя публичного метода.
- `_Fire` public в Base → наследуется в Local/Global → должен быть в скомпилированной программе.
- Если объект деактивируется до срабатывания delay — в VRChat `SendCustomEventDelayedSeconds` **продолжит работу** даже если объект disabled. Потенциально нежелательное поведение.

---

## Known Issues & Risks (приоритет)

### 🔴 Критично

| # | Проблема | Где | Диагноз |
|---|---------|-----|---------|
| 1 | `_actions[]` пустой в runtime | PSS_ChannelBase._Fire | RescanActions не сохраняется в scene до билда. Editor-side timing UdonSharp CopyUdonToProxy обнуляет поле после AddComponent |
| 2 | Virtual dispatch через typed PSS_ChannelLocal | PSS_TriggerBase.Fire() | `channel.Trigger()` на Global канале — UdonSharp cross-behaviour вызов резолвит по имени на реальном UdonBehaviour. Скорее всего OK, но **нужна верификация** |
| 3 | `_Dispatch()` private в Base | PSS_ChannelBase | В C# private методы не наследуются, но `Trigger()` в том же классе — компилятор должен включить. Риск: UdonSharp может не скомпилировать правильно. |

### 🟡 Важно

| # | Проблема | Где |
|---|---------|-----|
| 4 | Buffer for Late Join воспроизводит событие, не состояние | Flow 6 |
| 5 | `_firstDone` в PSS_OnEnable теряется при Editor reload | PSS_OnEnable |
| 6 | PSS_OnEnable `skipFirst=false` — неожиданное поведение для новых пользователей | UX |
| 7 | RescanActions нет на build/save хука — нужен IProcessSceneWithReport | Editor |
| 8 | Delay + object disable = event всё равно стреляет | PSS_ChannelBase._Dispatch |

### 🟢 Незначительно

| # | Проблема |
|---|---------|
| 9 | PSS_ActiveCustomTrigger name-search требует ручного `candidates[]` |
| 10 | PSS_StateSync applyOnStart логика может запутать |

---

## Verification Checklist

Проходить в таком порядке (VRChat Client Sim):

- [ ] **F1** — Local Interact → SetActive toggle (verify `_actions` populated)
- [ ] **F2** — Local Zone Enter → SetActive (verify OnPlayerTriggerEnter fires)
- [ ] **F3** — OnEnable(skipFirst=true) → SetActive при явном enable
- [ ] **F4** — OnTimer(interval=2, repeat=true) → Debug.Log action (verify loop)
- [ ] **F5** — Global Interact → SetActive (two client sim instances)
- [ ] **F6** — Global + Buffer (late join gets correct event)
- [ ] **F7** — StateSync Bool Toggle → correct state on late join
- [ ] **F8** — CustomTrigger chain A→B → B's action fires
- [ ] **F9** — Delay=2sec → action fires 2sec later

---

## Fixes Needed (по приоритету)

### Fix A — Build-time RescanActions hook ✅ v0.3.6
`Editor/PSS_BuildValidator.cs` → `IProcessSceneWithReport.OnProcessScene()` (callbackOrder=-10):
- Сканирует scene через `GetRootGameObjects()` → `GetComponentsInChildren<PSS_ChannelLocal>(true)`
- Для каждого канала: фильтрует `PSS_ActionBase[]` по `a.channel == channel`, заполняет `_actions`
- `UdonSharpEditorUtility.CopyProxyToUdon(channel)` → данные попадают в Udon heap до UdonSharp processing
- Запускается ПЕРЕД UdonSharp (callbackOrder < 0)

### Fix B — `_Dispatch` → `protected` ✅ v0.3.6
`PSS_ChannelBase._Dispatch()` переведён из `private` в `protected`.

### Fix C — Verify virtual dispatch
Тест в Client Sim: PSS_ChannelGlobal на объекте, `channel` typed Local, проверить что Global.Trigger() вызывается (добавить Debug.Log в оба).

### Fix D — PSS_OnEnable: `skipFirst=true` по умолчанию ✅ v0.3.6
`skipFirst = true` — не стрелять при старте сцены (более интуитивно).
⚠️ Breaking change: существующие сетапы где нужен OnEnable при старте — снять галочку вручную.
