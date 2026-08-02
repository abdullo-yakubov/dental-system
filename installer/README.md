# Инсталлятор DentalLocal

Реализует раздел 13 ТЗ (критерий приёмки §21.8): единый Windows-инсталлятор через Inno Setup,
который копирует приложение, создаёт ярлыки на рабочем столе и в меню «Пуск», регистрируется
в «Установка и удаление программ» и при удалении отдельно спрашивает про данные пациентов.

## Предварительные требования

- [Inno Setup 6+](https://jrsoftware.org/isinfo.php) (`ISCC.exe` должен быть в PATH, либо укажите полный путь при вызове).
- .NET SDK (для `dotnet publish`) — тот же, что используется для обычной сборки репозитория.
- Портативный дистрибутив PostgreSQL, распакованный в `Database/pgsql/` в корне репозитория
  (папки `bin/`, `lib/`, `share/` и т.д.) — **до** сборки инсталлятора. Инсталлятор подхватит их
  автоматически; если папка пуста, шаг просто пропускается (`skipifsourcedoesntexist`), но тогда
  `PostgresBootstrapService` не найдёт `pg_ctl`/`initdb` на целевой машине и не поднимет БД —
  для рабочего офлайн-инсталлятора класть бинарники обязательно.

## Сборка

```powershell
# 1. Публикация Server + Client (WASM, смёрженный в Server/wwwroot) + Launcher в installer\build\*
powershell -File installer\build.ps1

# 2. Сборка самого инсталлятора (итог — installer\output\DentalLocal-Setup-<версия>.exe)
ISCC installer\DentalLocal.iss /DAppVersion=1.0.0
```

## Раскладка после установки

```
{app}\                          ← например C:\Program Files\DentalLocal
├─ DentalLocal.Server.exe       ← self-contained publish Server (+ wwwroot со смёрженным Client)
├─ appsettings.json
├─ Launcher\
│  └─ DentalLocal.Launcher.exe  ← ярлыки на рабочем столе/в «Пуск» указывают сюда
├─ Database\
│  ├─ pgsql\                    ← портативный PostgreSQL (из шага сборки)
│  └─ data\                     ← создаётся при первом запуске (initdb), PostgresBootstrapService
├─ Data\Files\                  ← вложения пациентов (создаётся Server-ом при старте)
└─ Logs\                        ← Serilog, ротация 30 дней
```

Это соответствует тому, как `PostgresBootstrapService.FindProjectRootDir()` уже резолвит пути:
в production-раскладке (нет `src/` и `DentalLocal.slnx` рядом) он падает обратно на
`ContentRootPath` сервера, то есть на `{app}` — поэтому `Database/pgsql` и `Database/data`
должны лежать именно рядом с `DentalLocal.Server.exe`, как в схеме выше.

## Удаление

Стандартный Uninstaller Inno Setup через «Установка и удаление программ». Логи (`Logs/`)
удаляются всегда. Перед удалением показывается диалог «Удалить также данные пациентов
(база, вложения, бэкапы)?» с вариантом по умолчанию «Нет» — при «Да» дополнительно удаляются
`Database/data`, `Data` и `Backups`.

## Известные ограничения

- Скрипт не подписывает `.exe` (code signing) — сертификат у заказчика не запрашивался.
- `appsettings.json` в publish-выводе Server содержит дефолтную строку подключения
  (`Database=dental;Username=dental;Password=CHANGE_ME`, порт 5544) — она уже согласована с
  дефолтными параметрами `PostgresBootstrapService`, менять вручную не требуется для локальной
  однопользовательской установки.
