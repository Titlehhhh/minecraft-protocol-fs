## Core Primitives

Schema type → C# method:

- varint  → ReadVarInt / WriteVarInt           (variable-length int32, MOST COMMON)
- varlong → ReadVarLong / WriteVarLong
- i8      → ReadSignedByte / WriteSignedByte
- u8      → ReadUnsignedByte / WriteUnsignedByte
- i16     → ReadSignedShort / WriteSignedShort
- u16     → ReadUnsignedShort / WriteUnsignedShort
- i32     → ReadSignedInt / WriteSignedInt      (fixed 4-byte int, rare)
- u32     → ReadUnsignedInt / WriteUnsignedInt
- i64     → ReadSignedLong / WriteSignedLong
- u64     → ReadUnsignedLong / WriteUnsignedLong
- f32     → ReadFloat / WriteFloat
- f64     → ReadDouble / WriteDouble
- string  → ReadString / WriteString
- uuid    → ReadUUID / WriteUUID
- bool    → ReadBoolean / WriteBoolean
- pstring → same as string (ReadString / WriteString)

