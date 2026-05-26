# PSS — Оперативные заметки

## GitHub

- **Репо:** https://github.com/Pururut114/puru-signals-system
- **Публичное,** MIT лицензия
- **VPM listing:** https://Pururut114.github.io/puru-signals-system/index.json
- **VCC install URL:** `vcc://vpm/add-repo?url=https://Pururut114.github.io/puru-signals-system/index.json`
- **Package ID:** `com.pururut.pss`

---

## Рабочий процесс — новый релиз

```powershell
# 0. Прогнать валидатор
python _validate_release.py

# 1. Обновить version в package.json ПЕРВЫМ (до коммита и тега!)
# 2. Обновить CHANGELOG.md
git add .
git commit -m "release: PSS v0.X.X"
git push origin main
git tag v0.X.X
git push origin v0.X.X
# → release.yml создаёт zip + Release
# → build-listing.yml тригерится автоматически (workflow_run)
# → index.json обновляется на GitHub Pages
```

**Критично:** обновить `package.json` до создания тега. Если тег создан на коммите без обновлённой версии — удалить тег:
```
git push origin --delete vX.X.X && git tag -d vX.X.X
```

Следить за Actions: `gh run watch`

---

## GitHub Actions

### `release.yml`
Триггер: `git push origin v*` → создаёт `.zip` + GitHub Release. Node.js читает `package.json`.

### `build-listing.yml`
Триггер: `workflow_run` после успешного `release.yml`.  
**Важно:** триггер `release: published` НЕ работает — GitHub блокирует события от GITHUB_TOKEN. Используется `workflow_run`.

---

## VPM пакет — критические правила

Пакет ДОЛЖЕН включать:
1. `.meta` файлы для всех папок и файлов (стабильные GUIDs)
2. `UdonSharpProgramAsset` (`.asset`) рядом с каждым UdonSharpBehaviour `.cs` (кроме conditional assemblies)
3. `UdonSharpAssemblyDefinition` рядом с каждым `.asmdef` содержащим UdonSharpBehaviour скрипты

**UdonSharpAssemblyDefinition файлы:**

| Файл | sourceAssembly GUID |
|------|---------------------|
| `Runtime/com.pururut.pss.runtime.asset` | `7a88ebb6e79e416aafca4c0ca8e43eb2` |
| `Modules/com.pururut.pss.modules.asset` | `9156639969144b4ca1e25bb63f431b55` |
| `Modules/LTCGI/com.pururut.pss.ltcgi.asset` | `a39a74dfe5b54f5f896ccf3002d99a74` |
| `Modules/ProTV/com.pururut.pss.protv.asset` | `b5660d25c6f248cfbad7a7cd9f37d14a` |

- `m_Script` GUID UdonSharpAssemblyDefinition.cs: `5136146375e9a0a498a72a0091b40cc1`
- fileID для AssemblyDefinitionAsset ссылок: `5897886265953266890`
- `UdonSharpProgramAsset` GUID (из VRChat пакета): `c333ccfdd0cbdbc4ca30cef2dd6e6b9b`

**Генераторы (gitignored):**
- `_gen_meta_assets.py` — запускать при добавлении новых UdonSharpBehaviour скриптов
- `_validate_release.py` — проверяет версию, changelog, program assets, meta файлы

---

## Conditional assemblies — правило

- Если assembly имеет `defineConstraints` → `UdonSharpAssemblyDefinition` включить МОЖНО
- `UdonSharpProgramAsset` файлы для её скриптов в репо **НЕ включать** (иначе type=null → цикл ошибок)
- Пользователи с зависимостью создают program assets через PSS_AutoSetup (автоматически при domain reload)

---

## Assembly Definition структура

| Assembly | Папка | Назначение |
|----------|-------|------------|
| `com.pururut.pss.runtime` | `Runtime/` | Base классы, атрибуты, DataSlot, ConditionalTrigger, Channel, Network |
| `com.pururut.pss.modules` | `Modules/` (без LTCGI, ProTV) | Core, Player, Pickup, Physics, Avatar, Standalone |
| `com.pururut.pss.ltcgi` | `Modules/LTCGI/` | LTCGI интеграция, `defineConstraints: ["PSS_LTCGI_INSTALLED"]` |
| `com.pururut.pss.protv` | `Modules/ProTV/` | ProTV интеграция, `defineConstraints: ["PSS_PROTV_INSTALLED"]` |
| `com.pururut.pss.editor` | `Editor/` | Wizard, SpawnMenu, Setup, editor tools |

**Нюансы:**
- `PSS_ConditionalTrigger` в `Runtime/Data/` (не в Modules!) — во избежание circular dependency с DataSlot
- `com.pururut.pss.editor.asmdef` ссылается на `"UdonSharp.Editor"` (не `"UdonSharpEditor"` — это namespace)
- `com.pururut.pss.editor.asmdef` НЕ ссылается на ltcgi/protv assemblies — иначе падает без них
- ProTV зависит от двух assembly: `ArchiTech.ProTV.Runtime` + `ArchiTech.SDK.Runtime` (ATEventHandler живёт там)

---

## PSS_AutoSetup.cs (Editor/PSS_AutoSetup.cs)

На каждом domain reload:
- Детектит ProTV и LTCGI через reflection (`AppDomain.CurrentDomain.GetAssemblies()`)
- Добавляет/убирает `PSS_PROTV_INSTALLED` / `PSS_LTCGI_INSTALLED` через `PlayerSettings.SetScriptingDefineSymbolsForGroup()`
- Если defines стабильны → тихо создаёт missing program assets

## PSS_Setup.cs

Repair-only режим: `Tools > PSS > Repair Missing Program Assets`.  
`[InitializeOnLoad]` удалён — не запускается автоматически.  
Перед созданием проверяет все существующие assets через `FindAssets("t:UdonSharpProgramAsset")` — не создаёт дубликаты.

---

## Checklist нового модуля

- Trigger/Action → добавить в `Editor/PSS_Wizard.cs`
- **Standalone Utility → добавить в `Editor/PSS_SpawnMenu.cs`** (часто забывают)
- Conditional assembly → добавить в `PSS_AutoSetup.SyncDefines()` + `_validate_release.py`
- `_gen_meta_assets.py` запустить
- `package.json` + `CHANGELOG.md` обновить

---

## Документация в репо

- `Docs/modules.md` — реестр всех модулей
- `Docs/ADDING_MODULES.md` — как добавлять модули
- `Docs/STANDALONE_UTILITIES.md` — standalone утилиты
- `Docs/CHEATSHEET.md` — шпаргалка
- `ARCHITECTURE.md` — архитектура
- `CHANGELOG.md` — история версий
