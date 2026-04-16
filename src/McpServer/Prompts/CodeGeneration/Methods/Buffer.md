## Buffers

**ProtocolSerializationExtensions:**
- `reader.ReadBuffer(int length)`
- `reader.ReadBuffer(LengthFormat lengthFormat = LengthFormat.VarInt)` → `byte[]`
- `writer.WriteBuffer(ReadOnlySpan<byte> buff, LengthFormat lengthFormat = LengthFormat.VarInt)`
- `writer.WriteRestBuffer(ReadOnlySpan<byte> buff)` - Just copies the array 
- `writer.ReadRestBuffer()` - Reads the remaining bytes from the packet. After the call, the packet is empty.


**Examples:**
```csharp
// Read buffer with VarInt-prefix:
byte[] data = reader.ReadBuffer(LengthFormat.VarInt);

// Write buffer with VarInt-prefix:
writer.WriteBuffer(Data, LengthFormat.VarInt);

// Read buffer with manual length control
int len = reader.ReadVarInt();
byte[] data1 = reader.ReadBuffer(len);
byte[] data2 = reader.ReadBuffer(500);
```

**LengthFormat variants:** `LengthFormat.VarInt`(Most Popular), `LengthFormat.Byte`, `LengthFormat.Short`, `LengthFormat.Int`
