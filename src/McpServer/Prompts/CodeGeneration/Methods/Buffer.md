## Buffer (raw byte arrays)

**MinecraftPrimitiveReader/Writer (L1 — без LengthFormat):**
- `reader.ReadBuffer(int length)` → `byte[]`      — читает ровно `length` байт
- `reader.ReadRestBuffer()` → `byte[]`             — читает все оставшиеся байты
- `writer.WriteBuffer(ReadOnlySpan<byte>)`         — пишет сырые байты БЕЗ префикса длины

**ProtocolSerializationExtensions (L2 — с LengthFormat):**
- `reader.ReadBuffer(LengthFormat lengthFormat = LengthFormat.VarInt)` → `byte[]`
  Читает длину (по формату) + байты одним вызовом.
- `writer.WriteBuffer(ReadOnlySpan<byte> buff, LengthFormat lengthFormat = LengthFormat.VarInt)`
  Пишет длину (по формату) + байты одним вызовом.
- `writer.WriteBuffer<VarInt>(ReadOnlySpan<byte> buff)` — то же что VarInt, через generic
- `writer.WriteBuffer(ReadOnlySpan<byte> buff, int length)` — пишет только первые `length` байт (без префикса)

**Примеры:**
```csharp
// Читать буфер с VarInt-префиксом (1 вызов — L2):
byte[] data = reader.ReadBuffer(LengthFormat.VarInt);

// Писать буфер с VarInt-префиксом (1 вызов — L2):
writer.WriteBuffer(Data, LengthFormat.VarInt);

// Читать буфер с ручным контролем длины (2 вызова — L1):
int len = reader.ReadVarInt();
byte[] data = reader.ReadBuffer(len);
```

**LengthFormat варианты:** `LengthFormat.VarInt`, `LengthFormat.Byte`, `LengthFormat.Short`, `LengthFormat.Int`
