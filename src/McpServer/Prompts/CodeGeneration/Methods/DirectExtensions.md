## Direct Extension Methods

These are direct extension methods — **NOT registered in ReadType<T>/WriteType<T>**.
Do NOT use them with ReadArray/WriteArray. Use a manual loop instead.

**Entity metadata** (`ProtocolSerializationExtensions.EntityMetadata.cs`):
- `reader.ReadEntityMetadataEntry(protocolVersion)` → `EntityMetadataEntry`
- `writer.WriteEntityMetadataEntry(EntityMetadataEntry value, protocolVersion)`

**Death location** (`ProtocolSerializationExtensions.SpawnInfo.cs`):
- `reader.ReadDeathLocation(protocolVersion)` → `DeathLocation`
- `writer.WriteDeathLocation(DeathLocation value, protocolVersion)`
