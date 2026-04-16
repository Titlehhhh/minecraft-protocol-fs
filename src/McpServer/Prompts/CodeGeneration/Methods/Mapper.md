## `mapper` type — use the underlying primitive

`["mapper", {"type": "<baseType>", "mappings": {"0": "name0", ...}}]`

The `mapper` wraps a primitive with named values. **Do NOT invent enums, classes, or custom types.**
Use the underlying primitive as the C# property type and read/write with the matching primitive method.

| Schema | C# type | Read/Write |
|--------|---------|------------|
| `["mapper", {"type": "varint", ...}]` | `int`   | ReadVarInt / WriteVarInt |
| `["mapper", {"type": "u8",     ...}]` | `byte`  | ReadUnsignedByte / WriteUnsignedByte |
| `["mapper", {"type": "i16",    ...}]` | `short` | ReadSignedShort / WriteSignedShort |
| `["mapper", {"type": "i32",    ...}]` | `int`   | ReadSignedInt / WriteSignedInt |

❌ **WRONG** — do not create enums:
```csharp
public MyEnum Action { get; set; }  // enum does not exist
```

✅ **CORRECT:**
```csharp
public int Action { get; set; }
writer.WriteVarInt(Action);
Action = reader.ReadVarInt();
```
