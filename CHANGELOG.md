# Changelog

## [0.1.6] — 2026-05-15

### Fixed
- Added `.meta` files and `UdonSharpProgramAsset` (`.asset`) files to the repository. This is the canonical VPM distribution approach (TLP, CyanTrigger, etc.) — assets ship inside the package with stable GUIDs, so UdonSharp can find them on any install without extra setup. Previously UdonSharp reported "does not belong to a U# assembly" because program assets were missing at import time.
- `PSS_Setup.cs` simplified to a fallback repair tool (`Tools > PSS > Repair Missing Program Assets`). No longer auto-runs on load; correctly skips scripts that already have a program asset anywhere in the project to avoid duplicates.

---

## [0.1.5] — 2026-05-15

### Fixed
- `PSS_Setup.cs` restored — creates UdonSharp program assets in `Assets/PuruSignals/ProgramAssets/` (not in immutable `Packages/` folder). Fixes "Unable to find valid U# program asset" error after VCC install. Uses reflection to avoid direct `VRC.Udon.Editor` dependency. Auto-runs on project load (`[InitializeOnLoad]`) and on PSS import (`AssetPostprocessor`). Manual run: `Tools > PSS > Create Missing Program Assets`.

---

## [0.1.4] — 2026-05-15

### Removed
- `PSS_Setup.cs` — несовместим с immutable VPM package folders, не нужен в UdonSharp 1.x (program assets создаются автоматически)

---

## [0.1.3] — 2026-05-15

### Fixed
- Build VPM Listing теперь корректно автотриггерится после Build Release (`workflow_run` вместо `release: published`)

---

## [0.1.2] — 2026-05-15

### Fixed
- Исправлена ссылка на UdonSharp editor assembly: `"UdonSharpEditor"` → `"UdonSharp.Editor"` в `com.pururut.pss.editor.asmdef`

---

## [0.1.1] — 2026-05-15

### Fixed
- `PSS_ConditionalTrigger` moved from `Modules` to `Runtime` — устранена circular dependency между `com.pururut.pss.runtime` и `com.pururut.pss.modules`
- Убрана прямая ссылка на `com.pururut.pss.ltcgi` из `com.pururut.pss.editor.asmdef` — устранена ошибка компиляции editor assembly в проектах без LTCGI
- LTCGI wizard support временно убран из PSS Quick Setup (будет добавлен обратно в отдельной editor assembly)

---

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
