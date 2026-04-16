## Array Helpers

**topBitSetTerminatedArray** — элементы закодированы в 7 битах, бит 7 = "есть ещё":
- `reader.ReadTopBitSetTerminatedArray()` → `byte[]`
- `writer.WriteTopBitSetTerminatedArray(ReadOnlySpan<byte> values)`

```csharp
byte[] data = reader.ReadTopBitSetTerminatedArray();
writer.WriteTopBitSetTerminatedArray(Data);
```

---

All array helpers are extension methods on `MinecraftPrimitiveReader` / `MinecraftPrimitiveWriter`.
`LengthFormat` defaults to `LengthFormat.VarInt` when omitted.

**Generic arrays** (primitives + registered complex types via ReadType<T>/WriteType<T>):
- `reader.ReadArray<T>(LengthFormat)` — primitives only (bool, byte, sbyte, short, ushort, int, uint, long, ulong, float, double, string, Guid)
- `reader.ReadArray<T>(LengthFormat, protocolVersion)` — primitives + complex types (Position, Slot, GameProfile, etc.)
- `writer.WriteArray<T>(ReadOnlySpan<T>)` — VarInt length prefix, primitives only
- `writer.WriteArray<T>(ReadOnlySpan<T>, LengthFormat)` — primitives only
- `writer.WriteArray<T>(ReadOnlySpan<T>, protocolVersion)` — VarInt length prefix, primitives + complex types
- `writer.WriteArray<T>(ReadOnlySpan<T>, LengthFormat, protocolVersion)` — primitives + complex types

**VarInt element arrays** (elements encoded as VarInt, not fixed-size int):
- `reader.ReadVarIntArray(LengthFormat)` → `int[]`
- `writer.WriteVarIntArray(ReadOnlySpan<int>)` — VarInt length prefix
- `writer.WriteVarIntArray(ReadOnlySpan<int>, LengthFormat)`

**VarLong element arrays**:
- `reader.ReadVarLongArray(LengthFormat)` → `long[]`
- `writer.WriteVarLongArray(ReadOnlySpan<long>)` — VarInt length prefix
- `writer.WriteVarLongArray(ReadOnlySpan<long>, LengthFormat)`

**2D arrays** (array of arrays):
- `reader.ReadArray2d<T>(outerFormat, innerFormat)` → `T[][]` — primitives only
- `reader.ReadArray2d<T>(outerFormat, innerFormat, protocolVersion)` → `T[][]` — complex types
- `writer.WriteArray2d<T>(ReadOnlySpan<T[]>, outerFormat, innerFormat)` — primitives only
- `writer.WriteArray2d<T>(ReadOnlySpan<T[]>, outerFormat, innerFormat, protocolVersion)` — complex types

**Edge cases** (custom structure per element — use a manual loop):
```csharp
// Read
int count = reader.ReadVarInt();
var items = new MyStruct[count];
for (int i = 0; i < count; i++)
    items[i] = new MyStruct { X = reader.ReadVarInt(), Y = reader.ReadString() };

// Write
writer.WriteVarInt(Items.Length);
foreach (var item in Items)
{
    writer.WriteVarInt(item.X);
    writer.WriteString(item.Y);
}
```

## ⚠️ WriteArray vs WriteType — common mistake

❌ Do NOT use WriteArray<T> for custom types without protocolVersion:
```csharp
writer.WriteArray(Items);  // WRONG for custom types
```

✅ CORRECT for arrays of custom types:
```csharp
writer.WriteArray<Position>(Positions, protocolVersion);
reader.ReadArray<Position>(LengthFormat.VarInt, protocolVersion);
```
