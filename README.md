# Puru Signals System

Event-driven framework for VRChat worlds — **Trigger → Channel → Action** pipeline.

## Install via VCC

Open **VCC → Settings → Packages → Add Repository** and paste:
```
https://Pururut114.github.io/puru-signals-system/index.json
```

Or use the one-click link (may not work on all systems):
```
vcc://vpm/add-repo?url=https://Pururut114.github.io/puru-signals-system/index.json
```

## Requirements

- VRChat Worlds SDK `>=3.1.0`
- UdonSharp `>=1.1.8`
- Unity Post Processing Stack v2 `>=3.2.2`
- [LTCGI](https://github.com/PiMaker/ltcgi) _(optional — `PSS_LtcgiControl`. Auto-detected on install.)_
- [ProTV](https://protv.dev) _(optional — `PSS_ProTVAccessGate`. Auto-detected on install.)_

## Modules

Full list: [`Docs/modules.md`](Docs/modules.md)

### Core
- **Triggers:** OnInteract, OnEnterTrigger, OnExitTrigger, OnTimer, OnSpawn, OnEnable, OnDisable, CustomTrigger, ConditionalTrigger
- **Actions:** SetActive, AnimationParam, CallMethod, ActiveCustomTrigger, SetDataSlot, SetStateSync
- **Data:** DataSlot (local), StateSync (synced), ChannelLocal, ChannelGlobal

### Player
- `PSS_TeleportPlayer` — teleport local player to a target Transform

### Pickup
- `PSS_SetPickupable` — enable/disable VRC_Pickup.pickupable, optional Drop() on disable

### Physics
- `PSS_MoveToPoint` — teleport a Transform to a destination point (position + optional rotation)

### Avatar
- `PSS_SetAvatarScale` — set avatar eye height: exact world-authoritative height or player-controlled min/max range

### LTCGI _(requires LTCGI, auto-enabled)_
- `PSS_LtcgiControl` — toggle LTCGI globally or per screen

### ProTV _(requires ProTV, auto-enabled)_
- `PSS_ProTVAccessGate` — ProTV-based access gate: panel teleport, avatar scaling, object/collider/pickup gating

## Quick Setup

`Tools > PSS > Quick Setup...` — Wizard for building Trigger → Channel → Action chains in a few clicks.

Spawn standalone utilities: `Tools > PSS > Spawn > ...`

## Architecture

[`ARCHITECTURE.md`](ARCHITECTURE.md) — design overview, module types, data flow.

## Changelog

[`CHANGELOG.md`](CHANGELOG.md)

## License

MIT
