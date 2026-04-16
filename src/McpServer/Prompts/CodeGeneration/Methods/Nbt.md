## NBT Helpers

From `ProtocolSerializationExtensions.Nbt.cs`. All methods require `protocolVersion` argument.
C# type: `NbtTag`.

- `reader.ReadNbtTag(protocolVersion)` / `writer.WriteNbtTag(NbtTag value, protocolVersion)`
- `reader.ReadOptionalNbtTag(protocolVersion)` / `writer.WriteOptionalNbtTag(NbtTag? value, protocolVersion)`
- `reader.ReadAnonymousNbtTag(protocolVersion)` / `writer.WriteAnonymousNbtTag(NbtTag value, protocolVersion)`
- `reader.ReadAnonOptionalNbtTag(protocolVersion)` / `writer.WriteAnonOptionalNbtTag(NbtTag? value, protocolVersion)`

`nbt` → ReadNbtTag / WriteNbtTag
`anonymousNbt` → ReadAnonymousNbtTag / WriteAnonymousNbtTag
`optionalNbt` → ReadOptionalNbtTag / WriteOptionalNbtTag
