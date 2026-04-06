## Core Primitives (MinecraftPrimitiveReader/Writer)

Schema type → C# method:

- varint → ReadVarInt / WriteVarInt          (variable-length int32, MOST COMMON for integers)
- varlong → ReadVarLong / WriteVarLong
- i8 → ReadSignedByte / WriteSignedByte
- u8 → ReadUnsignedByte / WriteUnsignedByte
- i16 → ReadSignedShort / WriteSignedShort
- u16 → ReadUnsignedShort / WriteUnsignedShort
- i32 → ReadSignedInt / WriteSignedInt     (fixed 4-byte int, rare)
- u32 → ReadUnsignedInt / WriteUnsignedInt
- i64 → ReadSignedLong / WriteSignedLong
- u64 → ReadUnsignedLong / WriteUnsignedLong
- f32 → ReadFloat / WriteFloat
- f64 → ReadDouble / WriteDouble
- string → ReadString / WriteString
- UUID → ReadUUID / WriteUUID
- bool → ReadBoolean / WriteBoolean
- buffer → ReadBuffer(len), ReadRestBuffer, WriteBuffer(ReadOnlySpan<byte>)

## Array Helpers

All array helpers are extension methods on `MinecraftPrimitiveReader` / `MinecraftPrimitiveWriter`.
`LengthFormat` defaults to `LengthFormat.VarInt` when omitted.

**Generic arrays** (primitives + registered complex types via ReadType<T>/WriteType<T>):
- `reader.ReadArray<T>(LengthFormat)` — primitives only (bool, byte, sbyte, short, ushort, int, uint, long, ulong, float, double, string, Guid)
- `reader.ReadArray<T>(LengthFormat, protocolVersion)` — primitives + complex types (Position, Slot, GameProfile, etc.)
- `writer.WriteArray<T>(ReadOnlySpan<T>)` — VarInt length prefix, primitives only
- `writer.WriteArray<T>(ReadOnlySpan<T>, LengthFormat)` — primitives only
- `writer.WriteArray<T>(ReadOnlySpan<T>, protocolVersion)` — VarInt length prefix, primitives + complex types
- `writer.WriteArray<T>(ReadOnlySpan<T>, LengthFormat, protocolVersion)` — primitives + complex types

**VarInt element arrays** (when elements are encoded as VarInt, not fixed-size int):
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

## Protocol-Specific Types (ReadType<T>/WriteType<T>)

These are registered in `Extensions/ProtocolSerializationExtensions.cs` and routed via
`ReadType<T>(protocolVersion)` / `WriteType<T>(value, protocolVersion)`.

**Geometric types:**

- Position, Vec2f, Vec3f, Vec3f64, Vec3i, Vec4f

**Slot family:**

- Slot, HashedSlot, UntrustedSlot, SlotComponent, UntrustedSlotComponent, SlotComponentType

**Common types:**

- BannerPattern, DataComponentMatchers, ExactComponentMatcher, GameProfile,
  ItemBlockProperty, ItemEffectDetail, ItemSoundEvent, MinecraftSimpleRecipeFormat,
  PackedChunkPos, ChatSession, Particle, ParticleData, PreviousMessages, ServerLinkType,
  SoundSource, Tags

**Slot component dependencies:**

- ArmorTrimMaterial, ArmorTrimPattern, BannerPatternLayer,
  EntityMetadataPaintingVariant, EntityMetadataWolfVariant, IDSet, InstrumentData,
  ItemBlockPredicate, ItemBookPage, ItemConsumeEffect, ItemFireworkExplosion,
  ItemPotionEffect, ItemSoundHolder, ItemWrittenBookPage, JukeboxSongData

**Entity metadata:**

- EntityMetadataEntry

**Packet common payloads:**

- PacketCommonAddResourcePack, PacketCommonClearDialog,
  PacketCommonCookieRequest, PacketCommonCookieResponse, PacketCommonCustomClickAction,
  PacketCommonCustomReportDetails, PacketCommonRemoveResourcePack,
  PacketCommonSelectKnownPacks, PacketCommonServerLinks, PacketCommonSettings,
  PacketCommonStoreCookie, PacketCommonTransfer

**Play helper types:**

- ChatType, ChatTypeParameterType, ChatTypes, ChatTypesHolder,
  PositionUpdateRelatives, RecipeBookSetting, RecipeDisplay, SlotDisplay, MovementFlags

## RegistryEntryHolder<T>

Supported value types are registered in `ProtocolSerializationExtensions.cs`:

- string, int, ArmorTrimMaterial, ArmorTrimPattern, BannerPattern,
  EntityMetadataPaintingVariant, EntityMetadataWolfVariant, InstrumentData,
  ItemSoundEvent, JukeboxSongData

## NBT Helpers

From `ProtocolSerializationExtensions.Nbt.cs`. All methods require `protocolVersion` argument.
Return/field type: `NbtTag` (requires `using McProtoNet.NBT;` in the packet file).

- `ReadNbtTag(protocolVersion)` / `WriteNbtTag(NbtTag value, protocolVersion)`
- `ReadOptionalNbtTag(protocolVersion)` / `WriteOptionalNbtTag(NbtTag? value, protocolVersion)`
- `ReadAnonymousNbtTag(protocolVersion)` / `WriteAnonymousNbtTag(NbtTag value, protocolVersion)`
- `ReadAnonOptionalNbtTag(protocolVersion)` / `WriteAnonOptionalNbtTag(NbtTag? value, protocolVersion)`

When using NBT, add `using McProtoNet.NBT;` as the first line before `{{usages}}`.

## Direct Extension Methods (NOT usable with ReadArray<T>/WriteArray<T>)

These are direct `ref`/plain extension methods — **not registered in ReadType<T>/WriteType<T>**.
Do NOT use them with ReadArray/WriteArray. Use a manual loop instead.

**Death location** (`ProtocolSerializationExtensions.SpawnInfo.cs`):
- `reader.ReadDeathLocation(protocolVersion)` → `DeathLocation`
- `writer.WriteDeathLocation(DeathLocation value, protocolVersion)`

**Length-prefixed buffer** (raw bytes with a VarInt length prefix):
- `writer.WriteBuffer<VarInt>(ReadOnlySpan<byte>)` — writes VarInt length then bytes

## container_id field (protocol 766+)

Do NOT invent a type called `ContainerID`. Use the actual primitive based on schema:
- `u8` → `ReadUnsignedByte / WriteUnsignedByte`, C# type `byte`
- `i16` → `ReadSignedShort / WriteSignedShort`, C# type `short`
- `varint` → `ReadVarInt / WriteVarInt`, C# type `int`