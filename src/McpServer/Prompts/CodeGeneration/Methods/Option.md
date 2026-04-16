## Option (optional field)

`["option", innerType]` — the value is optionally present, prefixed by a boolean flag.

C# property type: `InnerType?` (nullable).

```csharp
// Read:
Value = reader.ReadBoolean() ? reader.ReadVarInt() : null;   // for option(varint)

// Write:
writer.WriteBoolean(Value.HasValue);
if (Value.HasValue) writer.WriteVarInt(Value.Value);
```

For complex types:
```csharp
// Read:
if (reader.ReadBoolean())
    Value = reader.ReadType<Position>(protocolVersion);
else
    Value = null;

// Write:
writer.WriteBoolean(Value.HasValue);
if (Value.HasValue)
    writer.WriteType<Position>(Value.Value, protocolVersion);
```
