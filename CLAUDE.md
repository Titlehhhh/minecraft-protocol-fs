# Claude Code Instructions — PacketGenerator

## Про этот проект

MCP сервер поверх minecraft-data. Предоставляет инструменты для генерации C# пакетов для McProtoNet.

## Субмодули

После клонирования:
```
git submodule update --init
```

- `minecraft-data/` — данные протокола (версия зафиксирована, **не обновлять**)
- `toon-dotnet/` — форк с JsonNode support

## MCP инструменты

- `GetPackets(filter)` — список пакетов
- `GetTypes()` — список типов
- `GetType(id)` — схема типа в toon или json формате
- `generate_packet(id)` — генерация C# класса (id = `play.toClient.packet_name`)

## Запуск

```
cd src/McpServer && dotnet run
```

Или через `../mcprotonet-workspace/scripts/start-mcp.sh`
