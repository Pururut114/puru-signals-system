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
Также имеет `workflow_dispatch` — можно запустить вручную через GitHub UI или API.

### `build-listing.yml`
Триггер: `workflow_run` после успешного `release.yml`, также `workflow_dispatch`.  
Генерирует `index.json` из всех GitHub Releases → пушит напрямую в ветку `gh-pages`.  
**Важно:** триггер `release: published` НЕ работает — GitHub блокирует события от GITHUB_TOKEN. Используется `workflow_run`.

### GitHub Pages
Режим: **`legacy`** (branch-based), источник — ветка `gh-pages`, путь `/`.  
Ранее был `workflow` (deploy-pages), переключён 2026-05-26 из-за бага с `workflow_dispatch` (см. ниже).  
`github-pages` environment **удалён** — имел `branch_policy` protection rule, мешал деплою.  
В `gh-pages` обязательно должен быть `.nojekyll` — без него Jekyll builder падает.

**Если Pages не задеплоился после пуша в `gh-pages`** (ручной триггер):
```powershell
$TOKEN = "..."  # из git credential / см. memory github_token.md
Invoke-RestMethod -Method POST "https://api.github.com/repos/Pururut114/puru-signals-system/pages/builds" `
  -Headers @{"Authorization"="token $TOKEN"; "Accept"="application/vnd.github+json"}
# Проверить статус:
Invoke-RestMethod "https://api.github.com/repos/Pururut114/puru-signals-system/pages/builds/latest" `
  -Headers @{"Authorization"="token $TOKEN"; "Accept"="application/vnd.github+json"} | Select status, error
```
Статус GitHub: `https://www.githubstatus.com/`

---

## Инцидент v0.1.19 — 2026-05-26

**Что случилось:** после `git push origin v0.1.19` workflow `release.yml` не триггернулся. Причина неизвестна — тег на GitHub был, workflow активен, token с нужными scopes, но `workflow_dispatch` через API возвращал `500 Failed to run workflow dispatch` на оба workflow.

**Подозрение:** баг GitHub, связанный с `branch_policy` protection rule на `github-pages` environment. Окончательная причина не установлена.

**Как решили:**
1. Release v0.1.19 создан вручную через GitHub API + zip загружен как asset
2. `index.json` сгенерирован локально (тот же Python-скрипт из `build-listing.yml`)
3. Создана ветка `gh-pages` с `index.json`, Pages переключён в `legacy` режим
4. `build-listing.yml` переписан: вместо `actions/deploy-pages` — git push в `gh-pages`
5. `github-pages` environment удалён (имел `branch_policy` protection rule — мешал деплою)
6. В `gh-pages` добавлен `.nojekyll` (без него Jekyll builder падал с "Page build failed")

**Итог:** задеплоено 2026-05-26 вечером. VPM index содержит 0.1.19. GitHub был деградирован в течение дня — Pages билд завис, после восстановления GitHub задеплоился штатно.

**Если повторится (release.yml не триггернулся):**
```bash
TOKEN="..."  # git credential fill → password

# 1. Создать Release вручную
curl -X POST "https://api.github.com/repos/Pururut114/puru-signals-system/releases" \
  -H "Authorization: token $TOKEN" \
  -d '{"tag_name":"vX.X.X","name":"Puru Signals System X.X.X","body":"..."}'

# 2. Загрузить zip (создать его через Compress-Archive из папки репо)
curl -X POST "https://uploads.github.com/repos/Pururut114/puru-signals-system/releases/<ID>/assets?name=com.pururut.pss-X.X.X.zip" \
  -H "Authorization: token $TOKEN" -H "Content-Type: application/zip" \
  --data-binary "@/path/to/com.pururut.pss-X.X.X.zip"

# 3. Запустить build-listing через workflow_dispatch (если заработает)
#    или запустить Python-скрипт из build-listing.yml локально → закоммитить в gh-pages
```

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
