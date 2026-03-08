# Claude Code Instructions

## База знаний

В начале каждой сессии читать файлы из `../mcprotonet-knowledge/`:

- `DOMAIN.md` — предметная область, pipeline, Protodef формат
- `STATUS.md` — текущий статус генерации
- `PACKET_DESIGN.md` — правила дизайна пакетов и типов
- `TRICKS.md` — баги, приколы, workarounds

## Про этот проект

PacketGenerator — MCP сервер поверх minecraft-data.
Предоставляет инструменты для генерации C# пакетов для McProtoNet.

### Субмодули

После клонирования обязательно:
```
git submodule update --init
```

- `minecraft-data/` — данные протокола Minecraft (версия зафиксирована, не обновлять)
- `toon-dotnet/` — форк с JsonNode support

### MCP инструменты

- `GetPackets(filter)` — список пакетов по фильтру
- `GetTypes()` — список типов
- `generate_packet(id)` — генерация C# класса пакета (id = "play.toClient.packet_name")
