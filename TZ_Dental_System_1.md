# Техническое задание: Локальная стоматологическая информационная система (Dental IS)

Единый файл ТЗ для реализации через Claude Code. Один врач = один администратор. Полностью локально, без сервера, без интернета, без облака. Все данные принадлежат пользователю.

---

## 1. Стек и архитектура

- Backend: C#, .NET 9, ASP.NET Core (Minimal API или контроллеры)
- Frontend: Blazor WebAssembly (Hosted model, в одном решении с backend)
- БД: PostgreSQL (локальный инстанс)
- ORM: Entity Framework Core (Code First, миграции)
- Валидация: FluentValidation
- Маппинг: AutoMapper
- Логирование: Serilog (файлы + консоль, ротация логов)
- Медиатор: MediatR (CQRS: Commands/Queries + Handlers)
- DI: встроенный Microsoft.Extensions.DependencyInjection
- Архитектура: Clean Architecture, слои:
  - `Domain` — сущности, enum'ы, value objects, доменные исключения. Без зависимостей.
  - `Application` — CQRS команды/запросы, интерфейсы репозиториев, DTO, валидаторы, маппинг-профили, бизнес-логика use case'ов.
  - `Infrastructure` — реализация репозиториев, EF Core DbContext, миграции, файловая система, backup-сервис, шифрование.
  - `Persistence` (может быть частью Infrastructure) — конфигурации EF Core (IEntityTypeConfiguration для каждой сущности), миграции.
  - `WebApi` — ASP.NET Core, контроллеры/эндпоинты, middleware, Program.cs, автозапуск PostgreSQL, health checks.
  - `Client` — Blazor WebAssembly, страницы, компоненты, сервисы HTTP-клиента, state management.
  - `Shared` — DTO, контракты, enum'ы, используемые и клиентом и сервером.
- Принципы: SOLID, DRY, KISS, YAGNI, Repository Pattern, Unit of Work, Result Pattern (Result<T> вместо исключений для бизнес-ошибок), Global Exception Handling Middleware.
- Никаких циклических зависимостей между слоями. Domain ничего не знает о верхних слоях.

---

## 2. Принцип локального запуска

- Один исполняемый файл/установщик. Пользователь запускает — открывается браузер (или WebView2/окно) с интерфейсом.
- `WebApi` при старте (`Program.cs`):
  1. Проверяет, установлен ли и запущен ли локальный PostgreSQL (через `pg_ctl`/проверку порта 5432 или кастомного).
  2. Если PostgreSQL не запущен — запускает его как процесс (portable PostgreSQL, встроенный в дистрибутив приложения, без системной установки — использовать embedded postgres, например через распакованный portable-бинарник PostgreSQL, хранящийся в `Database/pgsql/`).
  3. Проверяет существование БД `dental_system`, если нет — создаёт.
  4. Применяет `Database.Migrate()` автоматически.
  5. Проверяет наличие администратора/PIN-кода в таблице настроек, если нет — создаёт запись с PIN по умолчанию `1998`.
  6. Создаёт файловую структуру папок (см. раздел 3), если её нет.
  7. Делает стартовый backup БД.
  8. Запускает Kestrel, открывает системный браузер по умолчанию на `http://localhost:5000` (или запускает через WebView2 host, если используется desktop-обёртка).
- Порт настраивается в `appsettings.json`, по умолчанию 5000 (fallback на следующий свободный, если занят).
- При закрытии приложения (через кнопку "Выход" в UI, которая шлёт сигнал на graceful shutdown): выполняется финальный backup, останавливается PostgreSQL-процесс.

---

## 3. Файловая структура на диске

```
DentalSystem/
├── App/                          # исполняемые файлы приложения
├── Database/
│   ├── pgsql/                    # portable PostgreSQL бинарники
│   └── data/                     # data directory PostgreSQL
├── Backups/
│   ├── daily/
│   │   └── backup_YYYY-MM-DD_HHmmss.backup
│   ├── pre-update/
│   └── manual/
├── Patients/
│   └── {PatientId}/
│       ├── Photos/
│       ├── XRays/
│       ├── CT/
│       ├── Documents/
│       └── Reports/
├── Reports/                      # сгенерированные отчёты клиники (не per-patient)
├── Logs/
│   └── log_YYYY-MM-DD.txt
├── Settings/
│   └── appsettings.local.json
├── Temp/
├── Cache/
├── Exports/
└── Imports/
```

- При создании пациента автоматически создаётся папка `Patients/{PatientId}/` со всеми подпапками.
- Все пути к файлам в БД хранятся относительными от корня `DentalSystem/`, чтобы приложение можно было переносить на другой диск/папку.

---

## 4. Модель данных (сущности PostgreSQL)

### 4.1 Patient (Пациент)
| Поле | Тип | Ограничения |
|---|---|---|
| Id | uuid | PK |
| FullName | varchar(255) | NOT NULL |
| Gender | enum(Male,Female) | NOT NULL |
| BirthDate | date | NOT NULL |
| Phone | varchar(20) | NOT NULL, индекс |
| SecondaryPhone | varchar(20) | NULL |
| Address | varchar(500) | NULL |
| Allergies | text | NULL |
| ChronicDiseases | text | NULL |
| Comments | text | NULL |
| DoctorNotes | text | NULL |
| PhotoPath | varchar(500) | NULL |
| RegistrationDate | timestamp | NOT NULL, default now() |
| LastVisitDate | timestamp | NULL |
| CardNumber | varchar(50) | UNIQUE, автогенерация |
| IsArchived | boolean | default false |
| CreatedAt / UpdatedAt | timestamp | audit |

Индексы: `FullName` (GIN trigram для быстрого поиска), `Phone`, `CardNumber`, `BirthDate`.

### 4.2 Visit (Посещение)
| Поле | Тип |
|---|---|
| Id | uuid PK |
| PatientId | uuid FK → Patient |
| VisitDate | timestamp NOT NULL |
| DoctorNotes | text |
| Status | enum(Scheduled, InProgress, Completed, Cancelled, NoShow) |
| TotalCost | numeric(10,2) |
| PaidAmount | numeric(10,2) |
| DebtAmount | numeric(10,2) computed |
| CreatedAt/UpdatedAt | timestamp |

Индекс: `PatientId`, `VisitDate`.

### 4.3 Procedure (Каталог процедур)
| Поле | Тип |
|---|---|
| Id | uuid PK |
| Name | varchar(255) NOT NULL |
| Description | text |
| Price | numeric(10,2) NOT NULL |
| DurationMinutes | int |
| ColorCategory | varchar(20) — hex цвет для UI |
| Category | varchar(100) — например "Терапия", "Хирургия", "Ортопедия" |
| IsActive | boolean default true |

### 4.4 VisitProcedure (Процедуры в рамках визита, many-to-many с доп. полями)
| Поле | Тип |
|---|---|
| Id | uuid PK |
| VisitId | uuid FK |
| ProcedureId | uuid FK |
| ToothNumber | int NULL (FDI notation) |
| SurfaceCode | varchar(10) NULL (например "M","D","O","B","L") |
| Price | numeric(10,2) — цена на момент визита (снапшот) |
| Notes | text |

### 4.5 Tooth (Зуб пациента — состояние на текущий момент)
| Поле | Тип |
|---|---|
| Id | uuid PK |
| PatientId | uuid FK |
| ToothNumber | int NOT NULL (FDI, 11-48, включая молочные 51-85) |
| Status | enum(Healthy, Caries, Filled, Crown, Implant, RootCanal, Extracted, Veneer, Cracked, Destroyed, Missing) |
| UpdatedAt | timestamp |

UNIQUE (PatientId, ToothNumber).

### 4.6 ToothSurfaceHistory (История изменений по поверхности зуба)
| Поле | Тип |
|---|---|
| Id | uuid PK |
| ToothId | uuid FK |
| SurfaceCode | varchar(10) — M(mesial), D(distal), O(occlusal), B(buccal), L(lingual/palatal), Incisal |
| Status | enum (тот же набор диагнозов/состояний) |
| Diagnosis | varchar(255) — текстовый диагноз |
| ICD10Code | varchar(20) NULL |
| ICD11Code | varchar(20) NULL |
| VisitId | uuid FK NULL — визит, в рамках которого сделано изменение |
| ChangedAt | timestamp NOT NULL |
| DoctorComment | text |

Это неизменяемый журнал (append-only) — записи никогда не удаляются и не редактируются, только добавляются новые.

### 4.7 TreatmentPlan (План лечения)
| Поле | Тип |
|---|---|
| Id | uuid PK |
| PatientId | uuid FK |
| Title | varchar(255) |
| CreatedAt | timestamp |
| Status | enum(Draft, Active, Completed, Cancelled) |
| Notes | text |

### 4.8 TreatmentPlanItem
| Поле | Тип |
|---|---|
| Id | uuid PK |
| TreatmentPlanId | uuid FK |
| ProcedureId | uuid FK |
| ToothNumber | int NULL |
| EstimatedPrice | numeric(10,2) |
| OrderIndex | int |
| IsCompleted | boolean default false |
| CompletedVisitId | uuid FK NULL |

### 4.9 Payment (Платёж)
| Поле | Тип |
|---|---|
| Id | uuid PK |
| PatientId | uuid FK |
| VisitId | uuid FK NULL |
| Amount | numeric(10,2) NOT NULL |
| PaymentMethod | enum(Cash, Card, Transfer, Other) |
| PaymentDate | timestamp |
| Type | enum(Payment, Refund, DebtPayoff) |
| Comment | text |
| CreatedAt | timestamp |

### 4.10 Debt (Долг — вычисляемая витрина, либо материализованное представление)
Рассчитывается как `SUM(Visit.TotalCost) - SUM(Payment.Amount WHERE Type IN (Payment, DebtPayoff)) + SUM(Payment.Amount WHERE Type = Refund)` per Patient. Реализовать как SQL VIEW `patient_debts` с полями: PatientId, FullName, Phone, TotalDebt, LastDebtDate, LastVisitId.

### 4.11 Attachment (Вложение — фото/рентген/КТ/документ)
| Поле | Тип |
|---|---|
| Id | uuid PK |
| PatientId | uuid FK |
| VisitId | uuid FK NULL |
| Type | enum(Photo, XRay, CT, Document, PDF, Video, Other) |
| FilePath | varchar(500) NOT NULL — относительный путь |
| FileName | varchar(255) |
| FileSizeBytes | bigint |
| UploadedAt | timestamp |
| Description | text |

### 4.12 Material (Материал/склад)
| Поле | Тип |
|---|---|
| Id | uuid PK |
| Name | varchar(255) |
| Unit | varchar(50) — шт/мл/г |
| QuantityInStock | numeric(10,2) |
| MinStockThreshold | numeric(10,2) — для алертов о низком остатке |
| PricePerUnit | numeric(10,2) |
| CreatedAt/UpdatedAt | timestamp |

### 4.13 MaterialUsage (Списание материала)
| Поле | Тип |
|---|---|
| Id | uuid PK |
| VisitId | uuid FK |
| MaterialId | uuid FK |
| QuantityUsed | numeric(10,2) |
| UsedAt | timestamp |

При сохранении визита с указанными материалами — автоматическое списание со склада через транзакцию (уменьшение `QuantityInStock`), с проверкой на отрицательный остаток (предупреждение, но не блокировка).

### 4.14 Prescription (Назначение/рецепт лекарства)
| Поле | Тип |
|---|---|
| Id | uuid PK |
| VisitId | uuid FK |
| MedicationName | varchar(255) |
| Dosage | varchar(255) |
| Instructions | text |
| CreatedAt | timestamp |

### 4.15 Reminder (Напоминание / контрольный визит)
| Поле | Тип |
|---|---|
| Id | uuid PK |
| PatientId | uuid FK |
| Title | varchar(255) |
| ReminderDate | timestamp |
| IsCompleted | boolean default false |
| Type | enum(FollowUp, Recall, Custom) |
| Notes | text |

### 4.16 AuditLog (Журнал действий — append-only)
| Поле | Тип |
|---|---|
| Id | uuid PK |
| EntityType | varchar(100) |
| EntityId | uuid |
| Action | enum(Create, Update, Delete, Restore) |
| ChangesJson | jsonb — старое/новое значение |
| Timestamp | timestamp |

### 4.17 Settings (Настройки, singleton-таблица)
| Поле | Тип |
|---|---|
| Id | uuid PK |
| PinCodeHash | varchar(255) — bcrypt-хэш PIN |
| ThemeName | varchar(50) — Light/Dark/Blue/Teal/Custom |
| AutoLockMinutes | int default 10 |
| AutoBackupEnabled | boolean default true |
| BackupRetentionDays | int default 30 |
| ClinicName | varchar(255) |
| DoctorFullName | varchar(255) |
| Currency | varchar(10) default "UZS" |

Все таблицы: PostgreSQL, все FK с `ON DELETE RESTRICT` для медицинских данных (запрет удаления пациента с историей), кроме черновиков (TreatmentPlan draft можно удалить). Мягкое удаление (`IsArchived`/`IsDeleted` boolean) вместо физического удаления везде, где касается медицинской истории.

---

## 5. CQRS / Application слой — команды и запросы (MediatR)

Для каждой сущности реализовать полный набор:

**Patients**: `CreatePatientCommand`, `UpdatePatientCommand`, `ArchivePatientCommand`, `GetPatientByIdQuery`, `SearchPatientsQuery` (пагинация, фильтры: имя, телефон, дата рождения, номер карты), `GetPatientTimelineQuery`.

**Visits**: `CreateVisitCommand`, `UpdateVisitCommand`, `CancelVisitCommand`, `CompleteVisitCommand`, `GetVisitByIdQuery`, `GetPatientVisitsQuery`, `GetUpcomingVisitsQuery` (для календаря).

**Teeth**: `UpdateToothStatusCommand`, `AddToothSurfaceRecordCommand`, `GetPatientOdontogramQuery`, `GetToothHistoryQuery`, `GetSurfaceHistoryQuery`.

**TreatmentPlans**: `CreateTreatmentPlanCommand`, `AddTreatmentPlanItemCommand`, `CompleteTreatmentPlanItemCommand`, `GetTreatmentPlanQuery`.

**Payments**: `RegisterPaymentCommand`, `RegisterRefundCommand`, `PayOffDebtCommand`, `GetPatientPaymentsQuery`, `GetDebtorsListQuery` (с фильтрами по сумме, дате).

**Attachments**: `UploadAttachmentCommand` (сохраняет файл на диск + запись в БД), `DeleteAttachmentCommand` (мягкое удаление), `GetPatientAttachmentsQuery`.

**Materials**: `CreateMaterialCommand`, `UpdateStockCommand`, `GetLowStockMaterialsQuery`, `GetMaterialUsageReportQuery`.

**Reminders**: `CreateReminderCommand`, `CompleteReminderCommand`, `GetActiveRemindersQuery`.

**Analytics**: `GetDashboardStatsQuery`, `GetRevenueByPeriodQuery`, `GetPopularProceduresQuery`, `GetPatientFlowStatsQuery`.

**Backup**: `CreateManualBackupCommand`, `RestoreBackupCommand`, `GetBackupsListQuery`, `ValidateBackupIntegrityCommand`.

**Auth**: `VerifyPinCommand`, `ChangePinCommand`, `LockSessionCommand`.

Каждая Command обрабатывается через `IRequestHandler`, возвращает `Result<T>` (успех/ошибка с сообщением, без выброса исключений на бизнес-уровне). Каждая команда с изменением данных — валидируется через `FluentValidation` Validator, который встраивается в MediatR pipeline (`ValidationBehavior`). Каждое изменение — логируется в `AuditLog` через `AuditLoggingBehavior` (pipeline behavior).

---

## 6. REST API (WebApi контроллеры)

Базовый путь `/api/v1/`. Для каждого модуля — контроллер, тонкий, делегирующий в MediatR:

- `PatientsController`: GET `/patients` (поиск+пагинация), GET `/patients/{id}`, POST `/patients`, PUT `/patients/{id}`, DELETE `/patients/{id}` (архивация), GET `/patients/{id}/timeline`.
- `VisitsController`: CRUD + GET `/visits/upcoming`.
- `TeethController`: GET `/patients/{id}/odontogram`, PUT `/patients/{id}/teeth/{number}`, POST `/patients/{id}/teeth/{number}/surfaces`, GET `/patients/{id}/teeth/{number}/history`.
- `TreatmentPlansController`: CRUD.
- `PaymentsController`: POST `/payments`, POST `/payments/refund`, GET `/patients/{id}/payments`, GET `/debtors`.
- `AttachmentsController`: POST `/patients/{id}/attachments` (multipart/form-data), GET `/attachments/{id}/download`, DELETE `/attachments/{id}`.
- `MaterialsController`: CRUD, GET `/materials/low-stock`.
- `RemindersController`: CRUD.
- `AnalyticsController`: GET `/analytics/dashboard`, GET `/analytics/revenue`, GET `/analytics/procedures`.
- `BackupController`: GET `/backups`, POST `/backups/create`, POST `/backups/restore/{id}`, GET `/backups/{id}/validate`.
- `AuthController`: POST `/auth/login` (PIN), POST `/auth/change-pin`, POST `/auth/lock`.
- `SettingsController`: GET/PUT `/settings`.

Все эндпоинты возвращают единый формат ответа: `{ success, data, error, validationErrors }`. Глобальный `ExceptionHandlingMiddleware` ловит необработанные исключения → 500 + лог в Serilog, никогда не отдаёт stack trace на клиент.

---

## 7. Blazor WebAssembly — экраны и компоненты

### 7.1 Страница входа (`/login`)
- Поле ввода PIN (маскированное, keypad-стиль как в мобильных приложениях, кнопки 0-9).
- Кнопка "Войти".
- При ошибке — тряска поля + сообщение "Неверный PIN".
- После 5 неудачных попыток — блокировка на 30 секунд с таймером.

### 7.2 Главная панель / Dashboard (`/`)
- Карточки сверху: Пациентов всего, Визитов сегодня, Доход за день/неделю/месяц/год, Должников, Сумма долга.
- График дохода по месяцам (линейный, Chart.js через JS interop или Blazor-компонент).
- Диаграмма популярных процедур (pie chart).
- Список "Ближайшие записи" (сегодня/завтра) с быстрым переходом в карточку пациента.
- Список "Последние пациенты".
- Виджет "Низкий остаток материалов" (если есть).

### 7.3 Список пациентов (`/patients`)
- Строка поиска (live-search с debounce 300ms) по ФИО/телефону/адресу/номеру карты.
- Таблица: Фото, ФИО, Телефон, Возраст, Последний визит, Долг (цветной бейдж если > 0), кнопки Открыть/Редактировать/Архивировать.
- Фильтры: по полу, по диапазону возраста, есть ли долг, дата регистрации.
- Кнопка "Новый пациент" → модальное окно/страница создания.
- Виртуальный скроллинг для больших списков (Virtualize компонент Blazor).

### 7.4 Карточка пациента (`/patients/{id}`)
Вкладки:
- **Обзор**: основные данные, фото, аллергии, хронические заболевания, кнопки "Редактировать", "Новый визит", "Архивировать".
- **Одонтограмма**: интерактивная схема/3D-модель зубов (см. раздел 8).
- **Визиты**: список визитов, таблица с датой, процедурами, стоимостью, статусом.
- **План лечения**: список планов, каждый — с чек-листом процедур и прогресс-баром выполнения.
- **Финансы**: история платежей, текущий долг, кнопка "Внести оплату", "Возврат".
- **Документы**: галерея вложений с фильтром по типу (Фото/Рентген/КТ/Документы), лайтбокс-просмотр изображений, скачивание.
- **Timeline**: хронологическая лента всех событий пациента (визиты, платежи, изменения зубов, документы) единым списком.

### 7.5 Календарь врача (`/calendar`)
- Месяц/неделя/день вид.
- Клик по слоту → создание визита.
- Цветовая индикация статуса визита (запланирован/завершён/отменён/неявка).
- Drag-and-drop переноса визита.

### 7.6 Должники (`/debtors`)
- Таблица из VIEW `patient_debts`: ФИО, Телефон, Сумма долга, Дата последнего визита, кнопка "Оплачено" (открывает модалку ввода суммы и способа оплаты) и "Открыть карточку".
- Сортировка по сумме долга.
- Экспорт в Excel/PDF.

### 7.7 Финансы/Аналитика (`/analytics`)
- Фильтр по периоду (день/неделя/месяц/год/произвольный диапазон).
- Графики: доход по времени, доход по процедурам, доход по врачу (в данном случае один врач, но заложить расширяемость).
- Таблица топ-процедур.
- Экспорт отчётов в PDF/Excel.
- Конструктор отчётов: выбор полей, фильтров, группировки → генерация таблицы + экспорт.

### 7.8 Склад (`/inventory`)
- Таблица материалов: название, остаток, единица, мин. порог (подсветка красным если ниже порога), цена за единицу.
- Кнопка "Добавить материал", "Пополнить остаток" (модалка ввода количества прихода).
- История списаний по материалу.

### 7.9 Каталог процедур (`/procedures`)
- Таблица процедур с ценами, категориями, длительностью, цветом.
- CRUD прямо в таблице (inline editing) или модалки.

### 7.10 Настройки (`/settings`)
- Смена PIN-кода.
- Тема оформления (Light/Dark/Blue/Teal + Custom color picker).
- Название клиники, ФИО врача, валюта.
- Управление автоблокировкой (таймаут в минутах).
- Резервное копирование: список бэкапов с датой/размером, кнопки "Создать сейчас", "Восстановить", "Скачать", "Проверить целостность".
- Журнал действий (AuditLog) — просмотр с фильтрами по дате/сущности.
- Импорт/экспорт данных.

### 7.11 Общие UI-компоненты
- `ConfirmDialog` — для необратимых действий (архивация, возврат).
- `ToastNotification` — успех/ошибка/предупреждение.
- `LoadingSpinner` / skeleton loaders.
- `SearchBox` с debounce.
- `DataTable` переиспользуемый компонент с сортировкой/пагинацией/фильтрами.
- `FileUploadZone` — drag&drop загрузки файлов с превью.
- `DateRangePicker`.

---

## 8. Одонтограмма / 3D-модель зубов

- Реализовать как интерактивный SVG (2D-одонтограмма, FDI-нумерация 11-48 для взрослых, 51-85 для детей) — приоритетный вариант как более простой в поддержке и надёжный локально, без внешних 3D-библиотек с проблемной производительностью в WASM.
- Каждый зуб — отдельный кликабельный `<g>`-элемент в SVG.
- Каждый зуб визуально разделён на 5 поверхностей (mesial, distal, occlusal/incisal, buccal/vestibular, lingual/palatal) — каждая поверхность кликабельна отдельно.
- Цветовая индикация статуса зуба/поверхности (здоров — белый/серый контур, кариес — коричневый, пломба — синий, коронка — жёлтый, имплант — серебристый, удалён — перечёркнут серым, пульпит/лечение каналов — красный, винир — голубой, трещина — оранжевая линия).
- Клик по поверхности → всплывающая панель с выбором диагноза/статуса из списка (см. enum в 4.6) + поле комментария + привязка к текущему/новому визиту → сохранение создаёт запись в `ToothSurfaceHistory` и обновляет `Tooth.Status`.
- Клик по зубу (не по поверхности) → показывает полную историю зуба (все записи из `ToothSurfaceHistory` по всем его поверхностям) в хронологическом порядке.
- Легенда цветов сбоку от одонтограммы.
- Переключатель "Взрослые зубы / Молочные зубы".
- Опционально (если останется время после реализации 2D) — 3D-модель через Three.js (npm-пакет, интеграция через JS interop в Blazor) с аналогичной логикой кликов по mesh-поверхностям зуба; 2D-одонтограмма остаётся основным и обязательным вариантом.

---

## 9. Диагнозы и классификаторы

- Встроенный справочник МКБ-10 (стоматологические разделы K00-K14) — таблица `Icd10Codes` (Code, Description).
- Встроенный справочник МКБ-11 (аналогичные разделы) — таблица `Icd11Codes`.
- FDI-нумерация зубов — константы/enum в коде, не требует отдельной таблицы.
- При выборе диагноза на поверхности зуба — автокомплит по МКБ-10/11 с быстрым поиском по коду или названию.

---

## 10. Резервное копирование

- Формат: `pg_dump` в custom-формате (`.backup`), выполняется через вызов portable `pg_dump.exe` из `Database/pgsql/bin/`.
- Автоматический backup: при старте приложения, при закрытии, раз в 24 часа (фоновый `IHostedService`/`BackgroundService` с таймером), перед восстановлением из бэкапа, перед применением новых миграций при обновлении версии приложения.
- Хранение: `Backups/daily/` — ежедневные, с ротацией (удаление старше `BackupRetentionDays`, настройка в Settings, по умолчанию 30 дней); `Backups/manual/` — созданные вручную, не удаляются автоматически; `Backups/pre-update/` — перед обновлением, хранятся последние 5.
- Целостность: после создания backup вычисляется SHA-256 хэш файла, сохраняется рядом в `.sha256`-файле и в БД (таблица `BackupRecords`: Id, FilePath, CreatedAt, SizeBytes, Sha256Hash, Type). При восстановлении — сверка хэша перед restore.
- Шифрование: backup-файл дополнительно шифруется AES-256 с ключом, производным от PIN-кода (или отдельного мастер-пароля), перед сохранением на диск.
- Восстановление: страница в Настройках со списком бэкапов, кнопка "Восстановить" → подтверждение (двойное, с предупреждением о перезаписи текущих данных) → расшифровка → проверка хэша → создание safety-backup текущего состояния → `pg_restore` → перезапуск приложения.
- Autosave: в UI все формы редактирования данных пациента — автосохранение черновика в `Cache/` каждые 10 секунд (для защиты от потери несохранённых изменений при сбое), с очисткой черновика при успешном сохранении в БД.

---

## 11. Безопасность

- PIN-код: хранится как bcrypt-хэш, не в открытом виде. По умолчанию `1998`.
- Сессия: JWT или простой session-токен, хранится в памяти Blazor (не в localStorage, чтобы не оставался при закрытии браузера) + refresh при активности.
- Автоблокировка: после `AutoLockMinutes` бездействия (нет действий мыши/клавиатуры) — экран блокируется, показывается страница ввода PIN поверх текущего состояния (без потери несохранённых данных благодаря autosave).
- Логи безопасности: попытки входа (успешные/неуспешные) пишутся в `AuditLog` с типом `SecurityEvent`.
- Защита от случайного удаления: везде мягкое удаление + `ConfirmDialog` с текстовым подтверждением для критичных операций (например, ввести "УДАЛИТЬ" в поле).
- Контроль целостности файлов: для вложений (Attachments) хранить SHA-256 хэш файла в БД, периодическая фоновая проверка на повреждение/отсутствие файла с уведомлением врача.

---

## 12. Производительность

- EF Core: `AsNoTracking()` для всех read-запросов, `Compiled Queries` для часто вызываемых запросов (поиск пациентов, получение одонтограммы), пагинация везде через `Skip/Take` с обязательным `OrderBy`.
- Индексы PostgreSQL: см. раздел 4 (созданы через Fluent API в `IEntityTypeConfiguration`), обязательно на всех FK-колонках и колонках, участвующих в поиске/сортировке.
- Кэширование: `IMemoryCache` для каталога процедур, справочников МКБ, настроек (редко изменяемые данные), инвалидация при изменении.
- Blazor: `Virtualize` компонент для длинных списков (пациенты, история визитов), lazy loading страниц через `@page` + code splitting по умолчанию в WASM.
- Изображения: генерация thumbnail при загрузке фото/рентгена (для быстрого превью в галерее), полноразмерный файл — только при открытии.

---

## 13. Установщик

- Создать Windows Installer через WiX Toolset или Inno Setup, единый `.exe`/`.msi`.
- Действия установщика:
  1. Копирование файлов приложения в `Program Files/DentalSystem/` (или выбранную папку).
  2. Копирование portable PostgreSQL в `Database/pgsql/`.
  3. Инициализация PostgreSQL data directory при первом запуске (через `initdb`).
  4. Создание структуры папок из раздела 3.
  5. Применение EF Core миграций при первом запуске приложения (не через установщик, а через код приложения при старте — надёжнее).
  6. Создание ярлыка на рабочем столе и в меню "Пуск".
  7. Регистрация в "Установка и удаление программ" Windows (с корректным Uninstaller, который спрашивает "удалить также данные пациентов?" отдельным чекбоксом — по умолчанию НЕ удалять данные).
- Один клик по ярлыку — запуск `DentalSystem.exe`, который сам поднимает backend, БД и открывает интерфейс.

---

## 14. Обработка ошибок и логирование

- Serilog: запись в файлы `Logs/log_YYYY-MM-DD.txt`, ротация ежедневная, хранение 30 дней, уровни Information/Warning/Error/Fatal.
- Global Exception Middleware в WebApi: ловит все необработанные исключения, логирует полный стек в Serilog, клиенту отдаёт безопасное сообщение.
- В Blazor: `ErrorBoundary` вокруг основного layout, кастомная страница ошибки с кнопкой "Обновить" и "Сообщить об ошибке" (сохраняет лог локально).
- Бизнес-ошибки (например, "нельзя удалить пациента с активными визитами") — через `Result<T>` с понятным сообщением, не через exceptions.

---

## 15. Тестирование

- Unit-тесты (xUnit) для всех Application handlers, валидаторов, доменной логики расчёта долгов/остатков материалов.
- Integration-тесты для репозиториев и контроллеров (с использованием Testcontainers для PostgreSQL или in-memory альтернативы для CI).
- UI-тесты ключевых сценариев Blazor (bUnit) — как минимум: создание пациента, создание визита, оплата, обновление статуса зуба.
- Покрытие критичных модулей (финансы, одонтограмма, backup) — не менее 80%.

---

## 16. Дизайн-система

- Стиль: Fluent Design 2 / Material Design 3 гибрид, с элементами glassmorphism (полупрозрачные карточки с блюром на фоновых панелях).
- Темы: Light, Dark, Blue, Teal + возможность задать кастомный акцентный цвет через color picker в Настройках — реализовать через CSS custom properties, переключаемые классом на `<html>`.
- Типографика: системный шрифт (Segoe UI Variable / Inter fallback), чёткая иерархия размеров (H1-H6, body, caption).
- Иконки: единая библиотека (например, Fluent UI Icons или Lucide) через SVG-спрайты.
- Анимации: плавные переходы между страницами (200-300ms ease), скелетоны при загрузке данных вместо спиннеров где уместно.
- Полная адаптивность интерфейса под разные размеры окна ноутбука (min-width 1024px как базовый таргет, так как это desktop-приложение).

---

## 17. Порядок реализации для Claude Code

Реализовать в единой .NET-солюшн со следующими проектами: `DentalSystem.Domain`, `DentalSystem.Application`, `DentalSystem.Infrastructure`, `DentalSystem.WebApi`, `DentalSystem.Client` (Blazor WASM), `DentalSystem.Shared`. Начать с Domain-сущностей и EF Core конфигураций → миграции → Application CQRS слой → WebApi контроллеры → Blazor UI по страницам из раздела 7, в порядке: Auth → Dashboard → Patients → Odontogram → Visits → Payments/Debtors → Analytics → Inventory → Settings/Backup. Каждый модуль должен быть рабочим end-to-end (БД → API → UI) перед переходом к следующему.
