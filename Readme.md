# PacketGenerator

> MCP-сервер для генерации C# пакетов Minecraft протокола с поддержкой множества версий

[![.NET](https://img.shields.io/badge/.NET-10.0-purple?logo=dotnet)](https://dotnet.microsoft.com/)
[![MCP](https://img.shields.io/badge/MCP-1.1.0-blue)](https://modelcontextprotocol.io/)
[![Protocol](https://img.shields.io/badge/Protocol-735--772-green)](https://wiki.vg/Protocol)

Вспомогательный инструмент для **[McProtoNet](https://github.com/Titlehhhh/McProtoNet)** — .NET библиотеки для работы с Minecraft протоколом. PacketGenerator автоматически генерирует типизированные C# классы для каждого пакета, используя данные из [PrismarineJS/minecraft-data](https://github.com/PrismarineJS/minecraft-data) и LLM в качестве движка генерации.

---

## Содержание

- [Как это работает](#как-это-работает)
- [Архитектура](#архитектура)
- [Запуск](#запуск)
- [MCP инструменты](#mcp-инструменты)
- [REST API](#rest-api)
- [Настройка моделей](#настройка-моделей)
- [Структура проекта](#структура-проекта)

---

## Как это работает

PacketGenerator — это MCP-сервер (Model Context Protocol), который предоставляет инструменты для генерации C# кода пакетов Minecraft протокола. Claude (или другой LLM-агент) вызывает эти инструменты, передаёт идентификатор пакета, а сервер возвращает готовый C# класс.

```
Claude / LLM агент
       │
       │  generate_packet("play.toClient.keep_alive")
       ▼
┌─────────────────────┐
│   PacketGenerator   │  ← MCP сервер (порт 5000)
│     MCP Server      │
└──────────┬──────────┘
           │
     ┌─────┴──────┐
     │            │
     ▼            ▼
minecraft-data   OpenRouter API
(схемы пакетов)  (LLM генерация)
```

**Для каждого пакета:**

1. Загружается схема из `minecraft-data` для версий 735–772 (1.16–1.21)
2. Рассчитывается сложность пакета и выбирается подходящая LLM модель
3. Собирается промпт с правилами генерации и схемой пакета
4. LLM генерирует C# класс
5. Постпроцессор добавляет `using`, namespace, атрибуты `[PacketInfo]`, `[PacketId]`
6. Готовый файл сохраняется и возвращается ссылка на скачивание

---

## Архитектура

### 3-уровневая маршрутизация моделей

Пакеты разной сложности отправляются разным моделям, чтобы экономить токены на простых случаях и не терять качество на сложных:

```
PacketComplexityScorer
        │
        ├── score ≤ 20  ──►  SmallModel   (например, gpt-4o-mini)
        │
        ├── score ≤ 50  ──►  MediumModel
        │
        └── score > 50  ──►  HeavyModel
                             (или возврат промптов Claude для ручной генерации)
```

**Факторы сложности:**
| Признак | Баллы |
|---------|-------|
| Не-null версионный диапазон | +10 |
| Каждое поле в диапазоне | +2 |
| Вложенные массивы | +20 |
| Mapper типы | +5 |
| Switch конструкции | +15 |
| Опциональные поля | +10 |
| Конфликт типов поля между версиями | +15 |

### Пайплайн генерации

```
generate_packet(id)
        │
        ▼
 ProtocolRepository
 (загрузка схемы + история версий)
        │
        ▼
  BuildPromptAsync
  ┌────────────────────────────────┐
  │  SystemPrompt.md               │  правила генерации C# классов
  │  AvailableMethods.md           │  справочник методов сериализации
  │  Sceleton.md                   │  шаблон структуры класса
  │  BasePrompt.md + схема пакета  │  пользовательский промпт
  └────────────────────────────────┘
        │
        ▼
  PacketComplexityScorer
  → выбор модели
        │
        ▼
  OpenRouter API (LLM)
        │
        ▼
  ExtractCode (```csharp ... ```)
        │
        ▼
  PacketPostProcessor
  → using, namespace, [PacketInfo], [PacketId], [ProtocolSupport]
        │
        ▼
  Artifact Storage
  → ссылка на скачивание
```

---

## Запуск

### Требования

- .NET 10 SDK
- OpenRouter API ключ (для генерации через LLM)

### Клонирование

```bash
git clone --recursive https://github.com/Titlehhhh/McProtoNet
cd PacketGenerator
```

> `--recursive` нужен для подмодулей `minecraft-data` и `toon-dotnet`

### Настройка API ключа

```bash
# через user secrets (рекомендуется)
cd src/McpServer
dotnet user-secrets set "OpenRouter:ApiKey" "sk-or-..."

# или через переменную окружения
export OPENROUTER_API_KEY="sk-or-..."
```

### Запуск сервера

```bash
# из папки PacketGenerator
scripts\start-mcp.bat

# или напрямую
cd src/McpServer
dotnet run
```

Сервер запустится на `http://localhost:5000`.

### Подключение к Claude Code

Добавьте в `.mcp.json` вашего проекта:

```json
{
  "mcpServers": {
    "mcproto": {
      "type": "http",
      "url": "http://localhost:5000/mcp"
    }
  }
}
```

---

## MCP инструменты

После подключения Claude получает доступ к следующим инструментам:

### `generate_packet(id)`

Генерирует C# класс для указанного пакета.

```
generate_packet("play.toClient.keep_alive")
```

**Возвращает:**
- `Name` — имя пакета
- `Code` — готовый C# код (для простых/средних пакетов)
- `Link` — ссылка для скачивания файла
- `TokenCount` — сколько токенов использовано
- `ComplexityScore` — оценка сложности
- `Model` — какая модель использовалась

> Для очень сложных пакетов вместо `Code` возвращаются `SystemPrompt` + `UserPrompt` — Claude генерирует код самостоятельно.

### `generate_packets_batch(ids[])`

Параллельная генерация нескольких пакетов. Ошибка одного пакета не останавливает остальные.

### `GetPackets(filter?)`

Список всех доступных пакетов с опциональной фильтрацией.

```
GetPackets("keep_alive")        # один токен
GetPackets("play|login")        # несколько токенов через |
```

### `GetPacket(id, format?)`

Схема пакета во всех версиях протокола (формат `toon` или `json`).

### `GetTypes()` / `GetType(id, format?)`

Список всех типов протокола и схема конкретного типа.

---

## REST API

Помимо MCP, сервер предоставляет REST API для ручного управления:

| Метод | Путь | Описание |
|-------|------|----------|
| `GET` | `/api/packets` | Список всех пакетов |
| `GET` | `/api/packets/{ns}/{dir}` | Пакеты в пространстве имён |
| `GET` | `/api/schema/{**id}` | Схема + оценка сложности |
| `POST` | `/api/prompt` | Превью промпта (без LLM вызова) |
| `POST` | `/api/generate` | Генерация одного пакета |
| `POST` | `/api/generate/batch` | Генерация нескольких пакетов |
| `GET` | `/api/config` | Текущая конфигурация моделей |
| `POST` | `/api/config` | Обновление конфигурации |
| `GET` | `/artifacts/{id}` | Скачать сгенерированный файл |

---

## Настройка моделей

Конфигурация хранится в `model-config.json` рядом с бинарником и редактируется через `/api/config`.

```json
{
  "SmallModel": "openai/gpt-4o-mini",
  "MediumModel": "openai/gpt-4o-mini",
  "HeavyModel": "",

  "SmallComplexityThreshold": 20,
  "HeavyComplexityThreshold": 50,

  "SmallThreshold": 1500,
  "HeavyThreshold": 4000,

  "Temperature": 0,
  "MaxOutputTokens": 4096,
  "ReasoningEffort": "",
  "InputFormat": "toon"
}
```

**Ключевые параметры:**
- `HeavyModel: ""` — сложные пакеты возвращаются Claude для ручной генерации
- `InputFormat` — `"toon"` (компактный) или `"json"` (стандартный)
- `ReasoningEffort` — `"low"` / `"medium"` / `"high"` / `"xhigh"` для моделей с reasoning
- `SmallThreshold` / `HeavyThreshold` — пороги по количеству токенов (дополнительный критерий)

Все модели указываются в формате OpenRouter: `"provider/model-name"`.

---

## Структура проекта

```
PacketGenerator/
├── src/
│   ├── McpServer/              # Основной MCP сервер
│   │   ├── Services/
│   │   │   ├── CodeGenerator.cs        # Генерация кода + маршрутизация LLM
│   │   │   ├── PacketPostProcessor.cs  # Постобработка (namespace, атрибуты)
│   │   │   └── ModelConfigService.cs   # Конфиг моделей
│   │   ├── Tools/
│   │   │   ├── CodeGenTool.cs          # MCP: generate_packet, batch
│   │   │   └── DataTool.cs             # MCP: GetPackets, GetTypes
│   │   ├── Prompts/CodeGeneration/
│   │   │   ├── SystemPrompt.md         # Правила генерации C# классов
│   │   │   ├── BasePrompt.md           # Шаблон пользовательского промпта
│   │   │   ├── Sceleton.md             # Шаблон структуры класса
│   │   │   └── AvailableMethods.md     # Справочник методов сериализации
│   │   ├── PacketComplexityScorer.cs   # Оценка сложности → выбор модели
│   │   ├── HistoryBuilder.cs           # Слияние версий пакета
│   │   └── Program.cs                  # Точка входа, DI, эндпоинты
│   ├── ProtoCore/              # Загрузка и валидация протокола
│   ├── Protodef/               # Система типов Protodef
│   └── MinecraftData/          # Разрешение путей minecraft-data
├── minecraft-data/             # Субмодуль: JSON данные протокола
├── toon-dotnet/                # Субмодуль: Toon формат (форк)
├── Generated/                  # Сгенерированные пакеты
└── PacketGenerator.slnx        # Solution файл
```

---

## Поддерживаемые версии протокола

| Версия протокола | Minecraft |
|-----------------|-----------|
| 735 | 1.16 |
| 736–755 | 1.16.x |
| ... | ... |
| 770–772 | 1.21.x |

Полный диапазон: **735–772** (47 версий протокола).

---

> Проект разрабатывается и поддерживается одним автором. PR принимаются только от него.
