# Changelog

## [0.1.0] — 2026-05-15

### Added

**Core pipeline**
- Trigger → Channel → Action architecture
- `PSS_ChannelLocal` / `PSS_ChannelGlobal` — local and network-synced channels
- `PSS_Network` — network dispatcher (required for Global channels)

**Triggers**
- `PSS_OnInteract` — player clicks the object
- `PSS_OnEnterTrigger` / `PSS_OnExitTrigger` — trigger collider enter/exit
- `PSS_OnTimer` — timer-based (repeat, random interval)
- `PSS_OnSpawn` — once on scene/object start
- `PSS_OnEnable` / `PSS_OnDisable` — on GameObject enable/disable
- `PSS_CustomTrigger` — named trigger, callable by reference or name
- `PSS_ConditionalTrigger` — fires when a DataSlot satisfies a condition

**Actions**
- `PSS_SetActive` — enable / disable / toggle GameObject
- `PSS_AnimationParam` — set Animator parameter (Trigger/Bool/Int/Float)
- `PSS_CallMethod` — call a public method on UdonBehaviour
- `PSS_ActiveCustomTrigger` — activate CustomTrigger by reference or name
- `PSS_SetDataSlot` — write to DataSlot (Set/Add/Sub/Mul/Div)
- `PSS_SetStateSync` — write to StateSync (SetTrue/False/Toggle, Set/Add/Sub)

**Data / Network**
- `PSS_DataSlot` — local data container (Bool/Int/Float/Vector3/String)
- `PSS_StateSync` — synced state (Bool/Int/Float), late-join safe

**Standalone Utilities**
- `PSS_ZoneEnableWhileInside` — enables/disables objects while local player is inside a trigger zone
- `PSS_FallZoneBlackoutTeleport` — fade to black → teleport → fade back on zone enter

**Player**
- `PSS_TeleportPlayer` — teleport local player to a Transform target

**LTCGI** _(requires LTCGI)_
- `PSS_LtcgiControl` — toggle LTCGI globally or per screen

**Editor**
- `PSS Quick Setup` wizard — `Tools > PSS > Quick Setup...`
- Spawn menu for Standalone Utilities — `Tools > PSS > Spawn > ...`
