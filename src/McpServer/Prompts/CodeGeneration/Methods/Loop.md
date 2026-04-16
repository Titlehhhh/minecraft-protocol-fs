## Loop (sentinel-terminated array)

**VarInt loop** — специализированный хелпер для массивов VarInt с sentinel-значением:
- `reader.ReadLoopVarInt(int endValue)` → `int[]`  — читает VarInts пока не встретит sentinel; sentinel не включается в результат
- `writer.WriteLoopVarInt(ReadOnlySpan<int> values, int endValue)` — пишет значения, затем sentinel

```csharp
// Read:
Values = reader.ReadLoopVarInt(endValue: 0);

// Write:
writer.WriteLoopVarInt(Values, endValue: 0);
```

**Entity metadata loop** — sentinel 0xFF, у него нет payload (не читать лишний байт после него):
```csharp
// Read:
var entries = new List<EntityMetadataEntry>();
while (true)
{
    byte index = reader.ReadUnsignedByte();
    if (index == 0xFF) break;
    // ... но ReadEntityMetadataEntry уже читает index внутри себя — используй:
    entries.Add(reader.ReadEntityMetadataEntry(protocolVersion));
}

// Write:
foreach (var entry in Entries)
    writer.WriteEntityMetadataEntry(entry, protocolVersion);
writer.WriteUnsignedByte(0xFF);
```

Для других типов элементов — ручной while-цикл по sentinel.
